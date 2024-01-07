namespace n3k0c4t.live2d.an1m4ti0n;

public class ImportedAnimationKeyframedTrack
{
    public string Name { get; set; }
    public string Target { get; set; }
    public List<ImportedKeyframe<float>> Curve = new List<ImportedKeyframe<float>>();
}