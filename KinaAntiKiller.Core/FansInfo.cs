namespace AntiKiller.Core
{
    public class FansInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FaceUrl { get; set; }
        public string Sign { get; set; }
        public bool IsVisiable { get; set; } = true;
        public DateTime FollowDate { get; set; }
        public override string ToString()
        {
            return $"{Name}<{Id}> ({FollowDate})";
        }
    }
}
