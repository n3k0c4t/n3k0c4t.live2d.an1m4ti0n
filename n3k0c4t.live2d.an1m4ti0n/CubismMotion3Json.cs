namespace n3k0c4t.live2d.an1m4ti0n;

public class CubismMotion3Json
{
    public int Version;
    public SerializableMeta Meta;
    public SerializableCurve[] Curves;
    public SerializableUserData[] UserData;

    public class SerializableMeta
    {
        public float Duration;
        public float Fps;
        public bool Loop;
        public bool AreBeziersRestricted;
        public int CurveCount;
        public int TotalSegmentCount;
        public int TotalPointCount;
        public int UserDataCount;
        public int TotalUserDataSize;
    };

    public class SerializableCurve
    {
        public string Target;
        public string Id;
        public List<float> Segments;
    };

    public class SerializableUserData
    {
        public float Time;
        public string Value;
    }
}