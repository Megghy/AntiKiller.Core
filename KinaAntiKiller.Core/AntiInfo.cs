namespace AntiKiller.Core
{
    public class AntiInfo
    {
        public long Id { get; set; }
        public long UId { get; set; }
        public DateTime UnfollowDate { get; set; }
        public DateTime FollowDate { get; set; }
        public string Name { get; set; }
        public string FaceUrl { get; set; }
        public string Sign { get; set; }
    }
}
