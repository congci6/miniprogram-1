using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCity.Editor
{
    public static class VisualAssetFactory
    {
        public const string RootFolder = "Assets/PocketCityGenerated";
        public const string MaterialsFolder = RootFolder + "/Materials";
        public const string TexturesFolder = RootFolder + "/Textures";

        [MenuItem("Pocket City/Create Visual Assets")]
        public static void CreateVisualAssets()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(TexturesFolder);

            CreateMaterial("VertexColorOverlay.mat", new Color32(255, 255, 255, 255), "Pocket City/Vertex Color Transparent");
            // REFERENCE_IMAGE_BRIGHT_CITY_PALETTE keeps generated materials close to the fresh low-poly mockup.
            CreateMaterial("Road.mat", new Color32(76, 88, 91, 255), null);
            CreateMaterial("RoadLine.mat", new Color32(244, 240, 185, 255), null);
            CreateMaterial("Residential.mat", new Color32(255, 204, 109, 255), null);
            CreateMaterial("Commercial.mat", new Color32(84, 178, 225, 255), null);
            CreateMaterial("MixedUse.mat", new Color32(91, 202, 155, 255), null);
            CreateMaterial("Office.mat", new Color32(122, 200, 231, 255), null);
            CreateMaterial("Industrial.mat", new Color32(226, 125, 83, 255), null);
            CreateMaterial("Service.mat", new Color32(244, 170, 107, 255), null);
            CreateMaterial("Utility.mat", new Color32(92, 184, 201, 255), null);
            CreateMaterial("Roof.mat", new Color32(248, 238, 204, 255), null);
            CreateMaterial("Window.mat", new Color32(213, 246, 236, 255), null);
            CreateMaterial("SoftShadow.mat", new Color32(82, 118, 96, 180), null);
            CreateMaterial("TreeTrunk.mat", new Color32(132, 96, 62, 255), null);
            CreateMaterial("TreeCanopy.mat", new Color32(86, 190, 83, 255), null);
            CreateMaterial("Rock.mat", new Color32(164, 178, 166, 255), null);
            CreateMaterial("Shore.mat", new Color32(237, 226, 151, 255), null);
            CreateMaterial("GrassGrid.mat", new Color32(197, 236, 132, 255), null);
            CreateMaterial("LockedArea.mat", new Color32(220, 239, 121, 255), null);
            CreateMaterial("TrafficPulse.mat", new Color32(244, 116, 71, 255), null);
            CreateMaterial("ServiceNeed.mat", new Color32(255, 196, 95, 255), null);
            CreateMaterial("PreviewOk.mat", new Color32(95, 202, 139, 210), null);
            CreateMaterial("PreviewBlocked.mat", new Color32(238, 99, 82, 220), null);

            CreateZonePaletteTexture();
            CreateHeatPaletteTexture();
            CreateBuildingIconAtlas();
            CreateLoadingBackground();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created Pocket City visual assets under " + RootFolder);
        }

        public static Material LoadMaterial(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/" + fileName);
        }

        private static void CreateMaterial(string fileName, Color32 color, string preferredShader)
        {
            var assetPath = MaterialsFolder + "/" + fileName;
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            var shader = !string.IsNullOrEmpty(preferredShader) ? Shader.Find(preferredShader) : null;
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            material.color = color;
            material.SetColor("_Color", color);
            material.name = Path.GetFileNameWithoutExtension(fileName);
            EditorUtility.SetDirty(material);
        }

        private static void CreateZonePaletteTexture()
        {
            var colors = new[]
            {
                new Color32(96, 190, 122, 255),
                new Color32(88, 166, 226, 255),
                new Color32(82, 188, 158, 255),
                new Color32(112, 192, 214, 255),
                new Color32(222, 158, 86, 255),
                new Color32(244, 139, 124, 255),
                new Color32(82, 174, 186, 255),
            };

            var texture = new Texture2D(colors.Length * 64, 64, TextureFormat.RGBA32, false);
            for (var y = 0; y < texture.height; y += 1)
            {
                for (var x = 0; x < texture.width; x += 1)
                {
                    var index = Mathf.Clamp(x / 64, 0, colors.Length - 1);
                    texture.SetPixel(x, y, colors[index]);
                }
            }

            SaveTexture(texture, TexturesFolder + "/zone-palette.png", FilterMode.Point);
        }

        private static void CreateHeatPaletteTexture()
        {
            var texture = new Texture2D(256, 32, TextureFormat.RGBA32, false);
            var low = new Color32(92, 166, 220, 255);
            var mid = new Color32(96, 190, 122, 255);
            var high = new Color32(246, 226, 116, 255);
            for (var y = 0; y < texture.height; y += 1)
            {
                for (var x = 0; x < texture.width; x += 1)
                {
                    var t = x / 255f;
                    texture.SetPixel(x, y, t < 0.5f ? Lerp(low, mid, t * 2f) : Lerp(mid, high, (t - 0.5f) * 2f));
                }
            }

            SaveTexture(texture, TexturesFolder + "/heat-palette.png", FilterMode.Bilinear);
        }

        private static void CreateBuildingIconAtlas()
        {
            if (ImportExistingTextureAsset(TexturesFolder + "/building-icons.png", FilterMode.Bilinear))
            {
                return;
            }

            var texture = new Texture2D(1024, 640, TextureFormat.RGBA32, false);
            Fill(texture, new Color32(0, 0, 0, 0));

            DrawIcon(texture, 0, 0, new Color32(84, 170, 111, 255), IconShape.Home);
            DrawIcon(texture, 1, 0, new Color32(86, 139, 210, 255), IconShape.Shop);
            DrawIcon(texture, 2, 0, new Color32(205, 137, 70, 255), IconShape.Factory);
            DrawIcon(texture, 3, 0, new Color32(145, 111, 198, 255), IconShape.Tree);
            DrawIcon(texture, 4, 0, new Color32(88, 176, 196, 255), IconShape.Research);
            DrawIcon(texture, 5, 0, new Color32(188, 148, 72, 255), IconShape.Resource);
            DrawIcon(texture, 6, 0, new Color32(196, 132, 70, 255), IconShape.FreightRail);
            DrawIcon(texture, 7, 0, new Color32(188, 148, 72, 255), IconShape.Warehouse);
            DrawIcon(texture, 0, 1, new Color32(145, 111, 198, 255), IconShape.Cross);
            DrawIcon(texture, 1, 1, new Color32(86, 139, 210, 255), IconShape.Bus);
            DrawIcon(texture, 2, 1, new Color32(84, 155, 158, 255), IconShape.Bolt);
            DrawIcon(texture, 3, 1, new Color32(84, 155, 158, 255), IconShape.Drop);
            DrawIcon(texture, 4, 1, new Color32(178, 96, 190, 255), IconShape.Hospital);
            DrawIcon(texture, 5, 1, new Color32(118, 126, 205, 255), IconShape.CityHall);
            DrawIcon(texture, 6, 1, new Color32(80, 132, 205, 255), IconShape.Terminal);
            DrawIcon(texture, 7, 1, new Color32(210, 92, 82, 255), IconShape.Shelter);
            DrawIcon(texture, 0, 2, new Color32(84, 155, 158, 255), IconShape.Recycle);
            DrawIcon(texture, 1, 2, new Color32(145, 111, 198, 255), IconShape.Book);
            DrawIcon(texture, 2, 2, new Color32(215, 83, 72, 255), IconShape.Shield);
            DrawIcon(texture, 3, 2, new Color32(191, 151, 76, 255), IconShape.Truck);
            DrawIcon(texture, 0, 3, new Color32(80, 126, 205, 255), IconShape.Badge);
            DrawIcon(texture, 1, 3, new Color32(96, 166, 190, 255), IconShape.Office);
            DrawIcon(texture, 2, 3, new Color32(102, 178, 132, 255), IconShape.MixedUse);
            DrawIcon(texture, 3, 3, new Color32(208, 166, 86, 255), IconShape.Plaza);
            DrawIcon(texture, 4, 3, new Color32(87, 151, 211, 255), IconShape.Signal);
            DrawIcon(texture, 5, 3, new Color32(214, 174, 92, 255), IconShape.Wrench);
            DrawIcon(texture, 6, 3, new Color32(180, 160, 94, 255), IconShape.Parking);
            DrawIcon(texture, 7, 3, new Color32(186, 122, 202, 255), IconShape.Convention);
            DrawIcon(texture, 4, 2, new Color32(84, 155, 158, 255), IconShape.RainGarden);
            DrawIcon(texture, 5, 2, new Color32(72, 160, 210, 255), IconShape.Metro);
            DrawIcon(texture, 6, 2, new Color32(238, 192, 92, 255), IconShape.Solar);
            DrawIcon(texture, 7, 2, new Color32(214, 174, 92, 255), IconShape.WastePower);
            DrawIcon(texture, 0, 4, new Color32(210, 92, 82, 255), IconShape.Mail);
            DrawIcon(texture, 1, 4, new Color32(142, 154, 164, 255), IconShape.Memorial);

            SaveTexture(texture, TexturesFolder + "/building-icons.png", FilterMode.Point);
        }

        private static void CreateLoadingBackground()
        {
            if (ImportExistingTextureAsset(TexturesFolder + "/loading-background.png", FilterMode.Bilinear))
            {
                return;
            }

            var texture = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
            var top = new Color32(195, 229, 239, 255);
            var bottom = new Color32(134, 207, 142, 255);
            for (var y = 0; y < texture.height; y += 1)
            {
                var t = y / (texture.height - 1f);
                var color = Lerp(bottom, top, t);
                for (var x = 0; x < texture.width; x += 1)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            for (var i = 0; i < 32; i += 1)
            {
                var x = 80 + i * 29;
                var z = 310 + (i % 5) * 14;
                var h = 38 + (i % 7) * 12;
                FillRect(texture, x, z, 22 + (i % 3) * 8, h, i % 2 == 0 ? new Color32(88, 166, 226, 255) : new Color32(96, 190, 122, 255));
            }

            for (var i = 0; i < 10; i += 1)
            {
                FillRect(texture, 60 + i * 92, 270 + (i % 2) * 18, 70, 10, new Color32(116, 126, 128, 255));
            }

            SaveTexture(texture, TexturesFolder + "/loading-background.png", FilterMode.Bilinear);
        }

        private static bool ImportExistingTextureAsset(string assetPath, FilterMode filterMode)
        {
            if (!File.Exists(Path.GetFullPath(assetPath)))
            {
                return false;
            }

            AssetDatabase.ImportAsset(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = filterMode;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return true;
        }

        private static void DrawIcon(Texture2D texture, int cellX, int cellY, Color32 color, IconShape shape)
        {
            var x = cellX * 128;
            var y = texture.height - (cellY + 1) * 128;
            FillRect(texture, x + 8, y + 8, 112, 112, new Color32(22, 30, 38, 230));
            FillRect(texture, x + 18, y + 18, 92, 92, new Color32(35, 45, 56, 255));

            if (shape == IconShape.Home)
            {
                FillRect(texture, x + 40, y + 42, 48, 42, color);
                FillTriangle(texture, x + 30, y + 42, x + 64, y + 18, x + 98, y + 42, new Color32(230, 235, 225, 255));
            }
            else if (shape == IconShape.Shop)
            {
                FillRect(texture, x + 34, y + 44, 60, 38, color);
                FillRect(texture, x + 28, y + 34, 72, 14, new Color32(235, 222, 125, 255));
            }
            else if (shape == IconShape.Factory)
            {
                FillRect(texture, x + 28, y + 54, 72, 32, color);
                FillRect(texture, x + 74, y + 30, 14, 28, new Color32(80, 84, 88, 255));
                FillRect(texture, x + 34, y + 42, 18, 12, color);
            }
            else if (shape == IconShape.Tree)
            {
                FillRect(texture, x + 60, y + 58, 9, 28, new Color32(107, 79, 52, 255));
                FillCircle(texture, x + 64, y + 44, 30, color);
            }
            else if (shape == IconShape.Cross)
            {
                FillRect(texture, x + 56, y + 30, 16, 66, color);
                FillRect(texture, x + 31, y + 55, 66, 16, color);
            }
            else if (shape == IconShape.Hospital)
            {
                FillRect(texture, x + 30, y + 42, 68, 58, color);
                FillRect(texture, x + 42, y + 28, 44, 18, new Color32(236, 238, 242, 255));
                FillRect(texture, x + 58, y + 34, 12, 40, new Color32(225, 72, 82, 255));
                FillRect(texture, x + 44, y + 48, 40, 12, new Color32(225, 72, 82, 255));
                FillRect(texture, x + 38, y + 72, 14, 18, new Color32(236, 238, 242, 255));
                FillRect(texture, x + 76, y + 72, 14, 18, new Color32(236, 238, 242, 255));
                FillRect(texture, x + 56, y + 78, 16, 22, new Color32(72, 82, 96, 255));
            }
            else if (shape == IconShape.CityHall)
            {
                FillRect(texture, x + 28, y + 72, 72, 18, color);
                FillRect(texture, x + 34, y + 42, 60, 30, new Color32(236, 238, 226, 255));
                FillTriangle(texture, x + 28, y + 42, x + 64, y + 20, x + 100, y + 42, color);
                FillRect(texture, x + 42, y + 50, 8, 22, color);
                FillRect(texture, x + 60, y + 50, 8, 22, color);
                FillRect(texture, x + 78, y + 50, 8, 22, color);
                FillRect(texture, x + 44, y + 82, 40, 10, new Color32(50, 58, 72, 255));
            }
            else if (shape == IconShape.Bus)
            {
                FillRect(texture, x + 30, y + 42, 68, 34, color);
                FillRect(texture, x + 38, y + 50, 18, 14, new Color32(210, 230, 240, 255));
                FillRect(texture, x + 62, y + 50, 18, 14, new Color32(210, 230, 240, 255));
                FillCircle(texture, x + 44, y + 80, 8, new Color32(18, 22, 25, 255));
                FillCircle(texture, x + 84, y + 80, 8, new Color32(18, 22, 25, 255));
            }
            else if (shape == IconShape.Bolt)
            {
                FillTriangle(texture, x + 70, y + 20, x + 42, y + 68, x + 64, y + 64, color);
                FillTriangle(texture, x + 58, y + 62, x + 86, y + 62, x + 50, y + 104, color);
            }
            else if (shape == IconShape.Drop)
            {
                FillCircle(texture, x + 64, y + 70, 28, color);
                FillTriangle(texture, x + 64, y + 24, x + 42, y + 70, x + 86, y + 70, color);
            }
            else if (shape == IconShape.Recycle)
            {
                FillRect(texture, x + 36, y + 44, 56, 12, color);
                FillRect(texture, x + 36, y + 70, 56, 12, color);
                FillTriangle(texture, x + 92, y + 38, x + 108, y + 50, x + 92, y + 62, color);
                FillTriangle(texture, x + 36, y + 64, x + 20, y + 76, x + 36, y + 88, color);
            }
            else if (shape == IconShape.Book)
            {
                FillRect(texture, x + 30, y + 38, 68, 48, color);
                FillRect(texture, x + 62, y + 34, 4, 58, new Color32(240, 235, 210, 255));
                FillRect(texture, x + 38, y + 48, 18, 5, new Color32(240, 235, 210, 255));
                FillRect(texture, x + 72, y + 48, 18, 5, new Color32(240, 235, 210, 255));
                FillTriangle(texture, x + 64, y + 20, x + 28, y + 38, x + 100, y + 38, new Color32(240, 235, 210, 255));
            }
            else if (shape == IconShape.Shield)
            {
                FillTriangle(texture, x + 64, y + 24, x + 30, y + 42, x + 98, y + 42, color);
                FillRect(texture, x + 35, y + 42, 58, 28, color);
                FillTriangle(texture, x + 35, y + 70, x + 93, y + 70, x + 64, y + 102, color);
                FillRect(texture, x + 58, y + 44, 12, 42, new Color32(245, 236, 210, 255));
                FillRect(texture, x + 44, y + 58, 40, 12, new Color32(245, 236, 210, 255));
            }
            else if (shape == IconShape.Truck)
            {
                FillRect(texture, x + 26, y + 48, 54, 28, color);
                FillRect(texture, x + 80, y + 58, 22, 18, color);
                FillRect(texture, x + 84, y + 62, 12, 8, new Color32(225, 235, 235, 255));
                FillRect(texture, x + 34, y + 54, 28, 5, new Color32(245, 230, 160, 255));
                FillCircle(texture, x + 42, y + 82, 8, new Color32(18, 22, 25, 255));
                FillCircle(texture, x + 88, y + 82, 8, new Color32(18, 22, 25, 255));
            }
            else if (shape == IconShape.Badge)
            {
                FillCircle(texture, x + 64, y + 52, 30, color);
                FillTriangle(texture, x + 40, y + 72, x + 88, y + 72, x + 64, y + 104, color);
                FillRect(texture, x + 58, y + 34, 12, 44, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 42, y + 50, 44, 12, new Color32(235, 238, 220, 255));
            }
            else if (shape == IconShape.Office)
            {
                FillRect(texture, x + 36, y + 30, 56, 64, color);
                FillRect(texture, x + 44, y + 40, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 60, y + 40, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 76, y + 40, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 44, y + 58, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 60, y + 58, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 76, y + 58, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 58, y + 76, 12, 18, new Color32(40, 52, 64, 255));
            }
            else if (shape == IconShape.MixedUse)
            {
                FillRect(texture, x + 36, y + 34, 56, 58, color);
                FillRect(texture, x + 32, y + 58, 64, 16, new Color32(235, 208, 96, 255));
                FillTriangle(texture, x + 32, y + 34, x + 64, y + 16, x + 96, y + 34, new Color32(236, 239, 220, 255));
                FillRect(texture, x + 44, y + 42, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 62, y + 42, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 80, y + 42, 10, 10, new Color32(225, 236, 240, 255));
                FillRect(texture, x + 44, y + 76, 14, 12, new Color32(38, 48, 58, 255));
                FillRect(texture, x + 68, y + 76, 18, 12, new Color32(38, 48, 58, 255));
            }
            else if (shape == IconShape.Research)
            {
                FillRect(texture, x + 30, y + 56, 68, 34, color);
                FillRect(texture, x + 40, y + 34, 48, 24, new Color32(220, 238, 240, 255));
                FillRect(texture, x + 52, y + 24, 24, 12, color);
                FillCircle(texture, x + 44, y + 72, 7, new Color32(38, 56, 76, 255));
                FillCircle(texture, x + 64, y + 72, 7, new Color32(38, 56, 76, 255));
                FillCircle(texture, x + 84, y + 72, 7, new Color32(38, 56, 76, 255));
                FillRect(texture, x + 44, y + 70, 40, 4, new Color32(238, 210, 92, 255));
                FillCircle(texture, x + 64, y + 46, 6, new Color32(238, 210, 92, 255));
                FillRect(texture, x + 61, y + 46, 6, 28, new Color32(238, 210, 92, 255));
                FillTriangle(texture, x + 42, y + 34, x + 64, y + 16, x + 86, y + 34, new Color32(236, 239, 220, 255));
            }
            else if (shape == IconShape.Resource)
            {
                FillRect(texture, x + 28, y + 64, 72, 24, color);
                FillRect(texture, x + 36, y + 44, 22, 20, new Color32(116, 92, 58, 255));
                FillRect(texture, x + 62, y + 38, 28, 26, new Color32(142, 110, 68, 255));
                FillTriangle(texture, x + 34, y + 44, x + 48, y + 24, x + 62, y + 44, new Color32(238, 210, 92, 255));
                FillCircle(texture, x + 76, y + 52, 10, new Color32(96, 178, 118, 255));
                FillRect(texture, x + 72, y + 58, 8, 24, new Color32(74, 92, 64, 255));
                FillCircle(texture, x + 46, y + 88, 8, new Color32(38, 48, 58, 255));
                FillCircle(texture, x + 82, y + 88, 8, new Color32(38, 48, 58, 255));
            }
            else if (shape == IconShape.Plaza)
            {
                FillRect(texture, x + 28, y + 74, 72, 14, color);
                FillRect(texture, x + 54, y + 40, 20, 36, new Color32(226, 226, 210, 255));
                FillCircle(texture, x + 64, y + 34, 12, color);
                FillCircle(texture, x + 42, y + 62, 12, new Color32(96, 178, 118, 255));
                FillRect(texture, x + 38, y + 64, 8, 22, new Color32(87, 82, 58, 255));
                FillCircle(texture, x + 86, y + 62, 12, new Color32(96, 178, 118, 255));
                FillRect(texture, x + 82, y + 64, 8, 22, new Color32(87, 82, 58, 255));
            }
            else if (shape == IconShape.Convention)
            {
                FillRect(texture, x + 26, y + 58, 76, 34, color);
                FillRect(texture, x + 34, y + 38, 60, 22, new Color32(232, 226, 210, 255));
                FillTriangle(texture, x + 26, y + 38, x + 64, y + 18, x + 102, y + 38, color);
                FillRect(texture, x + 40, y + 48, 14, 12, new Color32(74, 92, 128, 255));
                FillRect(texture, x + 58, y + 48, 14, 12, new Color32(74, 92, 128, 255));
                FillRect(texture, x + 76, y + 48, 14, 12, new Color32(74, 92, 128, 255));
                FillRect(texture, x + 36, y + 70, 56, 8, new Color32(238, 210, 92, 255));
                FillCircle(texture, x + 44, y + 84, 6, new Color32(238, 210, 92, 255));
                FillCircle(texture, x + 64, y + 84, 6, new Color32(238, 210, 92, 255));
                FillCircle(texture, x + 84, y + 84, 6, new Color32(238, 210, 92, 255));
            }
            else if (shape == IconShape.Signal)
            {
                FillCircle(texture, x + 64, y + 40, 36, new Color32(87, 151, 211, 70));
                FillCircle(texture, x + 64, y + 40, 22, new Color32(87, 151, 211, 110));
                FillRect(texture, x + 60, y + 48, 8, 34, color);
                FillCircle(texture, x + 64, y + 86, 8, color);
                FillRect(texture, x + 42, y + 60, 8, 22, new Color32(150, 210, 230, 255));
                FillRect(texture, x + 78, y + 48, 8, 34, new Color32(150, 210, 230, 255));
                FillTriangle(texture, x + 44, y + 42, x + 64, y + 22, x + 84, y + 42, new Color32(210, 236, 240, 255));
            }
            else if (shape == IconShape.Wrench)
            {
                FillRect(texture, x + 38, y + 76, 58, 10, color);
                FillRect(texture, x + 76, y + 42, 10, 44, color);
                FillCircle(texture, x + 82, y + 40, 15, color);
                FillCircle(texture, x + 82, y + 40, 7, new Color32(35, 45, 56, 255));
                FillTriangle(texture, x + 36, y + 72, x + 52, y + 56, x + 62, y + 66, new Color32(230, 232, 210, 255));
                FillCircle(texture, x + 36, y + 84, 10, new Color32(40, 52, 64, 255));
            }
            else if (shape == IconShape.Parking)
            {
                FillRect(texture, x + 30, y + 28, 68, 72, color);
                FillRect(texture, x + 40, y + 38, 18, 52, new Color32(35, 45, 56, 255));
                FillRect(texture, x + 64, y + 38, 24, 12, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 64, y + 56, 22, 12, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 64, y + 74, 20, 12, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 42, y + 44, 12, 8, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 42, y + 58, 12, 8, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 42, y + 72, 12, 8, new Color32(235, 238, 220, 255));
            }
            else if (shape == IconShape.RainGarden)
            {
                FillRect(texture, x + 28, y + 74, 72, 14, new Color32(64, 112, 118, 255));
                FillCircle(texture, x + 48, y + 58, 18, new Color32(96, 178, 118, 255));
                FillCircle(texture, x + 76, y + 54, 22, color);
                FillTriangle(texture, x + 64, y + 26, x + 46, y + 64, x + 82, y + 64, new Color32(122, 190, 220, 255));
                FillCircle(texture, x + 64, y + 70, 9, new Color32(48, 84, 118, 255));
            }
            else if (shape == IconShape.Metro)
            {
                FillRect(texture, x + 34, y + 28, 60, 62, color);
                FillRect(texture, x + 42, y + 38, 44, 20, new Color32(210, 236, 242, 255));
                FillRect(texture, x + 46, y + 66, 36, 8, new Color32(35, 45, 56, 255));
                FillCircle(texture, x + 48, y + 86, 7, new Color32(18, 22, 25, 255));
                FillCircle(texture, x + 80, y + 86, 7, new Color32(18, 22, 25, 255));
                FillTriangle(texture, x + 38, y + 98, x + 52, y + 82, x + 58, y + 98, new Color32(235, 238, 220, 255));
                FillTriangle(texture, x + 70, y + 98, x + 76, y + 82, x + 90, y + 98, new Color32(235, 238, 220, 255));
            }
            else if (shape == IconShape.Terminal)
            {
                FillRect(texture, x + 28, y + 56, 72, 34, color);
                FillRect(texture, x + 36, y + 36, 56, 24, new Color32(226, 232, 220, 255));
                FillTriangle(texture, x + 28, y + 36, x + 64, y + 18, x + 100, y + 36, color);
                FillRect(texture, x + 42, y + 46, 16, 14, new Color32(210, 236, 242, 255));
                FillRect(texture, x + 70, y + 46, 16, 14, new Color32(210, 236, 242, 255));
                FillRect(texture, x + 56, y + 68, 16, 22, new Color32(35, 45, 56, 255));
                FillRect(texture, x + 30, y + 94, 68, 5, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 38, y + 102, 18, 5, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 72, y + 102, 18, 5, new Color32(235, 238, 220, 255));
            }
            else if (shape == IconShape.FreightRail)
            {
                FillRect(texture, x + 24, y + 34, 80, 24, new Color32(90, 102, 112, 255));
                FillRect(texture, x + 30, y + 40, 22, 12, color);
                FillRect(texture, x + 56, y + 40, 22, 12, new Color32(214, 174, 92, 255));
                FillRect(texture, x + 82, y + 40, 14, 12, new Color32(84, 155, 158, 255));
                FillRect(texture, x + 30, y + 66, 68, 22, color);
                FillRect(texture, x + 38, y + 72, 18, 10, new Color32(226, 236, 238, 255));
                FillRect(texture, x + 62, y + 72, 18, 10, new Color32(226, 236, 238, 255));
                FillCircle(texture, x + 42, y + 92, 6, new Color32(18, 22, 25, 255));
                FillCircle(texture, x + 86, y + 92, 6, new Color32(18, 22, 25, 255));
                FillRect(texture, x + 24, y + 100, 80, 5, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 32, y + 108, 14, 5, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 62, y + 108, 14, 5, new Color32(235, 238, 220, 255));
                FillRect(texture, x + 92, y + 108, 10, 5, new Color32(235, 238, 220, 255));
            }
            else if (shape == IconShape.Warehouse)
            {
                FillRect(texture, x + 26, y + 54, 76, 38, color);
                FillTriangle(texture, x + 22, y + 54, x + 64, y + 28, x + 106, y + 54, new Color32(142, 104, 72, 255));
                FillRect(texture, x + 34, y + 66, 20, 26, new Color32(226, 186, 104, 255));
                FillRect(texture, x + 58, y + 62, 18, 30, new Color32(214, 158, 82, 255));
                FillRect(texture, x + 80, y + 70, 16, 22, new Color32(226, 186, 104, 255));
                FillRect(texture, x + 28, y + 96, 72, 8, new Color32(90, 102, 112, 255));
                FillRect(texture, x + 38, y + 72, 10, 3, new Color32(130, 92, 60, 255));
                FillRect(texture, x + 64, y + 70, 8, 3, new Color32(130, 92, 60, 255));
                FillRect(texture, x + 84, y + 78, 8, 3, new Color32(130, 92, 60, 255));
            }
            else if (shape == IconShape.Shelter)
            {
                FillRect(texture, x + 28, y + 54, 72, 42, color);
                FillTriangle(texture, x + 22, y + 54, x + 64, y + 26, x + 106, y + 54, new Color32(152, 82, 74, 255));
                FillRect(texture, x + 54, y + 62, 20, 28, new Color32(246, 238, 220, 255));
                FillRect(texture, x + 42, y + 70, 44, 12, new Color32(246, 238, 220, 255));
                FillRect(texture, x + 32, y + 96, 64, 8, new Color32(72, 84, 88, 255));
            }
            else if (shape == IconShape.Solar)
            {
                FillCircle(texture, x + 76, y + 34, 18, color);
                FillRect(texture, x + 30, y + 62, 68, 30, new Color32(50, 104, 156, 255));
                FillRect(texture, x + 34, y + 66, 18, 10, new Color32(118, 188, 218, 255));
                FillRect(texture, x + 56, y + 66, 18, 10, new Color32(118, 188, 218, 255));
                FillRect(texture, x + 78, y + 66, 16, 10, new Color32(118, 188, 218, 255));
                FillRect(texture, x + 34, y + 80, 18, 8, new Color32(118, 188, 218, 255));
                FillRect(texture, x + 56, y + 80, 18, 8, new Color32(118, 188, 218, 255));
                FillRect(texture, x + 78, y + 80, 16, 8, new Color32(118, 188, 218, 255));
                FillRect(texture, x + 58, y + 92, 12, 12, new Color32(72, 84, 88, 255));
            }
            else if (shape == IconShape.WastePower)
            {
                FillRect(texture, x + 30, y + 62, 68, 28, new Color32(96, 116, 104, 255));
                FillRect(texture, x + 38, y + 42, 52, 22, color);
                FillRect(texture, x + 76, y + 28, 12, 34, new Color32(92, 94, 88, 255));
                FillRect(texture, x + 44, y + 50, 38, 6, new Color32(235, 238, 210, 255));
                FillTriangle(texture, x + 65, y + 18, x + 48, y + 54, x + 62, y + 51, new Color32(238, 210, 92, 255));
                FillTriangle(texture, x + 58, y + 50, x + 78, y + 50, x + 54, y + 84, new Color32(238, 210, 92, 255));
                FillRect(texture, x + 34, y + 88, 60, 8, new Color32(84, 155, 158, 255));
                FillTriangle(texture, x + 94, y + 82, x + 108, y + 92, x + 94, y + 102, new Color32(84, 155, 158, 255));
            }
            else if (shape == IconShape.Mail)
            {
                FillRect(texture, x + 28, y + 42, 72, 52, color);
                FillRect(texture, x + 34, y + 48, 60, 38, new Color32(238, 238, 222, 255));
                FillTriangle(texture, x + 34, y + 48, x + 64, y + 70, x + 94, y + 48, new Color32(224, 230, 218, 255));
                FillTriangle(texture, x + 34, y + 86, x + 64, y + 62, x + 94, y + 86, new Color32(218, 224, 214, 255));
                FillRect(texture, x + 44, y + 92, 40, 8, color);
                FillRect(texture, x + 54, y + 28, 20, 18, new Color32(90, 102, 112, 255));
                FillRect(texture, x + 42, y + 22, 44, 8, new Color32(90, 102, 112, 255));
            }
            else if (shape == IconShape.Memorial)
            {
                FillRect(texture, x + 28, y + 86, 72, 10, new Color32(74, 124, 94, 255));
                FillCircle(texture, x + 42, y + 78, 12, new Color32(96, 178, 118, 255));
                FillCircle(texture, x + 86, y + 78, 12, new Color32(96, 178, 118, 255));
                FillRect(texture, x + 42, y + 76, 44, 14, new Color32(176, 166, 132, 255));
                FillRect(texture, x + 50, y + 44, 28, 38, new Color32(218, 218, 204, 255));
                FillCircle(texture, x + 64, y + 44, 14, new Color32(218, 218, 204, 255));
                FillRect(texture, x + 56, y + 60, 16, 5, color);
                FillRect(texture, x + 54, y + 70, 20, 4, color);
                FillCircle(texture, x + 38, y + 66, 5, new Color32(238, 210, 92, 255));
                FillCircle(texture, x + 90, y + 66, 5, new Color32(238, 210, 92, 255));
                FillRect(texture, x + 36, y + 70, 4, 16, new Color32(78, 112, 72, 255));
                FillRect(texture, x + 88, y + 70, 4, 16, new Color32(78, 112, 72, 255));
            }
        }

        private static void SaveTexture(Texture2D texture, string assetPath, FilterMode filterMode)
        {
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = filterMode;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parts = assetPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i += 1)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void Fill(Texture2D texture, Color32 color)
        {
            for (var y = 0; y < texture.height; y += 1)
            {
                for (var x = 0; x < texture.width; x += 1)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            for (var yy = Mathf.Max(0, y); yy < Mathf.Min(texture.height, y + height); yy += 1)
            {
                for (var xx = Mathf.Max(0, x); xx < Mathf.Min(texture.width, x + width); xx += 1)
                {
                    texture.SetPixel(xx, yy, color);
                }
            }
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
        {
            var r2 = radius * radius;
            for (var y = centerY - radius; y <= centerY + radius; y += 1)
            {
                for (var x = centerX - radius; x <= centerX + radius; x += 1)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx + dy * dy <= r2 && x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void FillTriangle(Texture2D texture, int x1, int y1, int x2, int y2, int x3, int y3, Color32 color)
        {
            var minX = Mathf.Max(0, Mathf.Min(x1, Mathf.Min(x2, x3)));
            var maxX = Mathf.Min(texture.width - 1, Mathf.Max(x1, Mathf.Max(x2, x3)));
            var minY = Mathf.Max(0, Mathf.Min(y1, Mathf.Min(y2, y3)));
            var maxY = Mathf.Min(texture.height - 1, Mathf.Max(y1, Mathf.Max(y2, y3)));

            for (var y = minY; y <= maxY; y += 1)
            {
                for (var x = minX; x <= maxX; x += 1)
                {
                    if (PointInTriangle(x, y, x1, y1, x2, y2, x3, y3))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static bool PointInTriangle(int px, int py, int x1, int y1, int x2, int y2, int x3, int y3)
        {
            var d1 = Sign(px, py, x1, y1, x2, y2);
            var d2 = Sign(px, py, x2, y2, x3, y3);
            var d3 = Sign(px, py, x3, y3, x1, y1);
            var hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
            var hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNegative && hasPositive);
        }

        private static int Sign(int px, int py, int ax, int ay, int bx, int by)
        {
            return (px - bx) * (ay - by) - (ax - bx) * (py - by);
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }

        private enum IconShape
        {
            Home,
            Shop,
            Factory,
            Tree,
            Cross,
            Hospital,
            CityHall,
            Bus,
            Bolt,
            Drop,
            Recycle,
            Book,
            Shield,
            Truck,
            Badge,
            Office,
            MixedUse,
            Plaza,
            Signal,
            Wrench,
            Parking,
            RainGarden,
            Metro,
            Terminal,
            Solar,
            WastePower,
            Convention,
            Research,
            Resource,
            FreightRail,
            Warehouse,
            Shelter,
            Mail,
            Memorial
        }
    }
}
