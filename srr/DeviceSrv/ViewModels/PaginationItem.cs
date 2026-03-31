namespace DeviceSrv.ViewModels
{
    public class PaginationItem
    {
        public string Text { get; set; }
        public int Value { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsEllipsis { get; set; }
        public bool IsClickable => !IsEllipsis;
    }
}
