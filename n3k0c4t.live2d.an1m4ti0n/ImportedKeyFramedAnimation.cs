namespace n3k0c4t.live2d.an1m4ti0n;

public class ImportedKeyframedAnimation
{
    public string Name { get; set; }
    public float SampleRate { get; set; }
    public float Duration { get; set; }

    public List<ImportedAnimationKeyframedTrack> TrackList { get; set; } = new List<ImportedAnimationKeyframedTrack>();
    public List<ImportedEvent> Events = new List<ImportedEvent>();

    public ImportedAnimationKeyframedTrack FindTrack(string name)
    {
        var track = TrackList.Find(x => x.Name == name);
        if (track == null)
        {
            track = new ImportedAnimationKeyframedTrack { Name = name };
            TrackList.Add(track);
        }
        return track;
    }
}
