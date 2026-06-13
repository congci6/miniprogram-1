using System;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Simulation
{
    public sealed class CityGridCore
    {
        private readonly TileData[] tiles;
        private List<PlacedBuilding> buildingList;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool ExpansionUnlocked { get; set; }

        public void SetBuildingList(List<PlacedBuilding> buildings)
        {
            buildingList = buildings;
        }

        public CityGridCore(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Grid dimensions must be positive.");
            }

            Width = width;
            Height = height;
            tiles = new TileData[width * height];

            for (var y = 0; y < height; y += 1)
            {
                for (var x = 0; x < width; x += 1)
                {
                    var terrain = TerrainForPosition(x, y, width, height);
                    tiles[Index(new GridPos(x, y))] = new TileData
                    {
                        Terrain = terrain,
                        Zone = ZoneType.None,
                        LandValue = BaseLandValue(terrain, ZoneType.None)
                    };
                }
            }
        }

        public bool InBounds(GridPos pos)
        {
            return pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;
        }

        public void LockedExpansionBounds(out int startX, out int startY, out int endX, out int endY)
        {
            var width = Math.Max(7, Width / 4);
            var height = Math.Max(7, Height / 4);
            startX = Math.Max(2, Width - width - 1);
            startY = Math.Max(2, Height - height - 1);
            endX = Width - 1;
            endY = Height - 1;
        }

        public bool IsLockedExpansionTile(GridPos pos)
        {
            if (ExpansionUnlocked || !InBounds(pos))
            {
                return false;
            }

            int startX;
            int startY;
            int endX;
            int endY;
            LockedExpansionBounds(out startX, out startY, out endX, out endY);
            return pos.X >= startX && pos.X <= endX && pos.Y >= startY && pos.Y <= endY;
        }

        public bool RectTouchesLockedExpansion(GridPos pos, GridSize size)
        {
            if (!RectInBounds(pos, size))
            {
                return false;
            }

            foreach (var tilePos in PositionsInRect(pos, size))
            {
                if (IsLockedExpansionTile(tilePos))
                {
                    return true;
                }
            }

            return false;
        }

        public TileData GetTile(GridPos pos)
        {
            if (!InBounds(pos))
            {
                throw new ArgumentOutOfRangeException("pos", "Grid position out of bounds.");
            }

            return tiles[Index(pos)];
        }

        public string CanPlaceBuilding(GridPos pos, GridSize size)
        {
            if (!RectInBounds(pos, size))
            {
                return "建筑超出地图边界";
            }

            if (RectTouchesLockedExpansion(pos, size))
            {
                return "\u672a\u89e3\u9501\u533a\u57df";
            }

            foreach (var tilePos in PositionsInRect(pos, size))
            {
                var tile = GetTile(tilePos);
                if (tile.Terrain == TerrainType.Water)
                {
                    return "水面不能建造";
                }

                if (!string.IsNullOrEmpty(tile.BuildingId))
                {
                    return "地块已有建筑";
                }

                if (!string.IsNullOrEmpty(tile.RoadId))
                {
                    return "道路上不能建造建筑";
                }
            }

            return string.Empty;
        }

        public bool CanPlaceRoad(GridPos pos)
        {
            if (!InBounds(pos))
            {
                return false;
            }

            if (IsLockedExpansionTile(pos))
            {
                return false;
            }

            var tile = GetTile(pos);
            return tile.Terrain != TerrainType.Water && string.IsNullOrEmpty(tile.BuildingId);
        }

        public void OccupyBuilding(string buildingId, GridPos pos, GridSize size)
        {
            var reason = CanPlaceBuilding(pos, size);
            if (!string.IsNullOrEmpty(reason))
            {
                throw new InvalidOperationException(reason);
            }

            foreach (var tilePos in PositionsInRect(pos, size))
            {
                GetTile(tilePos).BuildingId = buildingId;
            }
        }

        public void RemoveBuilding(string buildingId)
        {
            for (var i = 0; i < tiles.Length; i += 1)
            {
                if (tiles[i].BuildingId == buildingId)
                {
                    tiles[i].BuildingId = string.Empty;
                }
            }
        }

        public void SetRoad(GridPos pos, string roadId)
        {
            if (!CanPlaceRoad(pos))
            {
                throw new InvalidOperationException("Road cannot be placed on this tile.");
            }

            var tile = GetTile(pos);
            tile.RoadId = roadId;
            tile.Zone = ZoneType.None;
        }

        public string CanSetZone(GridPos pos, ZoneType zone)
        {
            if (!InBounds(pos))
            {
                return "分区超出地图边界";
            }

            if (IsLockedExpansionTile(pos))
            {
                return "\u672a\u89e3\u9501\u533a\u57df";
            }

            var tile = GetTile(pos);
            if (tile.Terrain == TerrainType.Water)
            {
                return "水面不能设置分区";
            }

            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                return "道路不能设置分区";
            }

            return string.Empty;
        }

        public void SetZone(GridPos pos, ZoneType zone)
        {
            var reason = CanSetZone(pos, zone);
            if (!string.IsNullOrEmpty(reason))
            {
                throw new InvalidOperationException(reason);
            }

            GetTile(pos).Zone = zone;
        }

        public string ZoneReasonForBuilding(GridPos pos, GridSize size, ZoneType preferredZone)
        {
            if (preferredZone == ZoneType.None)
            {
                return string.Empty;
            }

            foreach (var tilePos in PositionsInRect(pos, size))
            {
                var tile = GetTile(tilePos);
                if (tile.Zone != ZoneType.None && tile.Zone != preferredZone)
                {
                    return "建筑类型与当前分区不匹配";
                }
            }

            return string.Empty;
        }

        public int CountZoneTiles(ZoneType zone)
        {
            var count = 0;
            for (var i = 0; i < tiles.Length; i += 1)
            {
                if (tiles[i].Zone == zone)
                {
                    count += 1;
                }
            }

            return count;
        }

        public void ResetDynamicTileValues()
        {
            for (var i = 0; i < tiles.Length; i += 1)
            {
                tiles[i].Traffic = 0;
                tiles[i].Pollution = 0;
                tiles[i].Noise = 0;
                tiles[i].TransitAccess = 0;
                tiles[i].LogisticsAccess = 0;
                tiles[i].ParkAccess = 0;
                tiles[i].HealthAccess = 0;
                tiles[i].DeathcareAccess = 0;
                tiles[i].EducationAccess = 0;
                tiles[i].WasteAccess = 0;
                tiles[i].SafetyAccess = 0;
                tiles[i].FireProtectionAccess = 0;
                tiles[i].SecurityAccess = 0;
                tiles[i].CommunicationAccess = 0;
                tiles[i].MailAccess = 0;
                tiles[i].RoadMaintenanceAccess = 0;
                tiles[i].ParkingAccess = 0;
                tiles[i].StormwaterAccess = 0;
                tiles[i].LandValue = BaseLandValue(tiles[i].Terrain, tiles[i].Zone);
            }
        }

        public void AddTilePressure(GridPos pos, int traffic, int pollution, int noise, int landValueDelta)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.Traffic = Math.Max(0, tile.Traffic + traffic);
            tile.Pollution = Math.Max(0, tile.Pollution + pollution);
            tile.Noise = Math.Max(0, tile.Noise + noise);
            tile.LandValue = Math.Max(0, Math.Min(100, tile.LandValue + landValueDelta));
        }

        public void AddTransitAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.TransitAccess = Math.Max(0, Math.Min(100, tile.TransitAccess + value));
        }

        public void AddLogisticsAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.LogisticsAccess = Math.Max(0, Math.Min(100, tile.LogisticsAccess + value));
        }

        public void AddParkAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.ParkAccess = Math.Max(0, Math.Min(100, tile.ParkAccess + value));
        }

        public void AddHealthAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.HealthAccess = Math.Max(0, Math.Min(100, tile.HealthAccess + value));
        }

        public void AddDeathcareAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.DeathcareAccess = Math.Max(0, Math.Min(100, tile.DeathcareAccess + value));
        }

        public void AddEducationAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.EducationAccess = Math.Max(0, Math.Min(100, tile.EducationAccess + value));
        }

        public void AddWasteAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.WasteAccess = Math.Max(0, Math.Min(100, tile.WasteAccess + value));
        }

        public void AddSafetyAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.SafetyAccess = Math.Max(0, Math.Min(100, tile.SafetyAccess + value));
        }

        public void AddFireProtectionAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.FireProtectionAccess = Math.Max(0, Math.Min(100, tile.FireProtectionAccess + value));
        }

        public void AddSecurityAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.SecurityAccess = Math.Max(0, Math.Min(100, tile.SecurityAccess + value));
        }

        public void AddCommunicationAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.CommunicationAccess = Math.Max(0, Math.Min(100, tile.CommunicationAccess + value));
        }

        public void AddMailAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.MailAccess = Math.Max(0, Math.Min(100, tile.MailAccess + value));
        }

        public void AddRoadMaintenanceAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.RoadMaintenanceAccess = Math.Max(0, Math.Min(100, tile.RoadMaintenanceAccess + value));
        }

        public void AddParkingAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.ParkingAccess = Math.Max(0, Math.Min(100, tile.ParkingAccess + value));
        }

        public void AddStormwaterAccess(GridPos pos, int value)
        {
            if (!InBounds(pos))
            {
                return;
            }

            var tile = GetTile(pos);
            tile.StormwaterAccess = Math.Max(0, Math.Min(100, tile.StormwaterAccess + value));
        }

        public bool IsInBounds(GridPos pos)
        {
            return InBounds(pos);
        }

        public PlacedBuilding GetBuildingAt(GridPos pos)
        {
            var id = FindBuildingIdAt(pos);
            if (string.IsNullOrEmpty(id) || buildingList == null) return null;
            foreach (var b in buildingList)
            {
                if (b.Id == id || (b.Pos.X == pos.X && b.Pos.Y == pos.Y))
                    return b;
            }
            return null;
        }

        public ZoneType GetZoneType(GridPos pos)
        {
            return InBounds(pos) ? GetTile(pos).Zone : ZoneType.None;
        }

        public void SetZoneType(GridPos pos, ZoneType type)
        {
            if (InBounds(pos)) GetTile(pos).Zone = type;
        }

        public RoadType GetRoadType(GridPos pos)
        {
            if (!InBounds(pos)) return RoadType.None;
            var roadId = GetTile(pos).RoadId;
            if (string.IsNullOrEmpty(roadId)) return RoadType.None;
            switch (roadId.ToLowerInvariant())
            {
                case "highway": return RoadType.Highway;
                case "boulevard": return RoadType.Boulevard;
                case "avenue": return RoadType.Avenue;
                default: return RoadType.Road;
            }
        }

        public void SetRoadType(GridPos pos, RoadType type)
        {
            if (!InBounds(pos)) return;
            SetRoad(pos, type.ToString().ToLowerInvariant());
        }

        public string FindBuildingIdAt(GridPos pos)
        {
            return InBounds(pos) ? GetTile(pos).BuildingId : string.Empty;
        }

        public IEnumerable<GridPos> PositionsInRect(GridPos pos, GridSize size)
        {
            for (var y = pos.Y; y < pos.Y + size.H; y += 1)
            {
                for (var x = pos.X; x < pos.X + size.W; x += 1)
                {
                    yield return new GridPos(x, y);
                }
            }
        }

        public IEnumerable<GridPos> AllPositions()
        {
            for (var y = 0; y < Height; y += 1)
            {
                for (var x = 0; x < Width; x += 1)
                {
                    yield return new GridPos(x, y);
                }
            }
        }

        private bool RectInBounds(GridPos pos, GridSize size)
        {
            return size.W > 0 &&
                   size.H > 0 &&
                   InBounds(pos) &&
                   InBounds(new GridPos(pos.X + size.W - 1, pos.Y + size.H - 1));
        }

        private int Index(GridPos pos)
        {
            return pos.Y * Width + pos.X;
        }

        private static TerrainType TerrainForPosition(int x, int y, int width, int height)
        {
            var dx = x - width * TerrainConstants.WaterThreshold;
            var dy = y - height * TerrainConstants.ShallowWaterThreshold;
            var waterBand = Math.Sin((x + y) * TerrainConstants.DeepWaterThreshold) * TerrainConstants.WaterNoiseScale + height * TerrainConstants.HillsNoiseScale;

            if (Math.Abs(y - waterBand) < 1.2 && x > width * TerrainConstants.HillsDetailScale && x < width * TerrainConstants.HillsBlendFactor)
            {
                return TerrainType.Water;
            }

            if (dx * dx + dy * dy < TerrainConstants.HillsElevationThreshold || (x > width * TerrainConstants.HillsPrimaryThreshold && y > height * TerrainConstants.HillsSecondaryThreshold))
            {
                return TerrainType.Hill;
            }

            return TerrainType.Plain;
        }

        private static int BaseLandValue(TerrainType terrain, ZoneType zone)
        {
            if (terrain == TerrainType.Water)
            {
                return 0;
            }

            var value = terrain == TerrainType.Hill ? 58 : 70;
            if (zone == ZoneType.Civic)
            {
                value += 6;
            }
            else if (zone == ZoneType.Office)
            {
                value += 2;
            }
            else if (zone == ZoneType.MixedUse)
            {
                value += 4;
            }
            else if (zone == ZoneType.Industrial || zone == ZoneType.Utility)
            {
                value -= 8;
            }

            return Math.Max(0, Math.Min(100, value));
        }
    }
}
