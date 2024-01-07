namespace n3k0c4t.live2d.an1m4ti0n;

public class ImportedKeyframe<T>
{
    public float time { get; set; }
    public T value { get; set; }
    public T inSlope { get; set; }
    public T outSlope { get; set; }
    public float[] coeff { get; set; }

    public ImportedKeyframe(float time, T value, T inSlope, T outSlope, float[] coeff)
    {
        this.time = time;
        this.value = value;
        this.inSlope = inSlope;
        this.outSlope = outSlope;
        this.coeff = coeff;
    }

    public float Evaluate(float sampleTime)
    {
        float t = sampleTime - time;
        return (t * (t * (t * coeff[0] + coeff[1]) + coeff[2])) + coeff[3];
    }
}
