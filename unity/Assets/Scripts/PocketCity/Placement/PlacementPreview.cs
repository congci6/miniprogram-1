using PocketCity.Core;

namespace PocketCity.Placement
{
    /// <summary>
    /// 建筑放置预览结果
    /// </summary>
    public struct PlacementPreview
    {
        public bool Ok;
        public string FailureReason;
        public GridPos Position;
        public GridSize Size;
        public int Cost;
    }
}
