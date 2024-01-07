// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AssetStudio;
using n3k0c4t.live2d.an1m4ti0n;
using n3k0c4t.live2d.an1m4ti0n.Extensions;
using Newtonsoft.Json;

var inputFile = args[0];
var outputFolder = args[1];

if (string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(outputFolder) || args.Length < 2)
{
    Console.WriteLine(Assembly.GetExecutingAssembly().FullName + " <Asset Map file>");
    return;
}

if (!File.Exists(inputFile))
{
    Console.WriteLine($"File not found: {outputFolder}");
    return;
}

if (!Directory.Exists(outputFolder))
{
    Console.WriteLine($"Destination path not found: {outputFolder}");
}

ResourceMap.FromFile(args[0]);

var manager = new AssetsManager
{
    Game = GameManager.GetGame(GameType.ProjectSekai),
    SpecifyUnityVersion = "2020.3.32f1"
};

var resources = ResourceMap.GetEntries()
    .Where(x => x.Container.Contains("/live2d/"))
    .Select(x => x.Source)
    .Distinct()
    .ToList();

var containers = new Dictionary<AssetStudio.Object, string>();

var paramDb = new List<string>();
var partsDb = new List<string>();

// Some motion asset uses parameters not exists in models
// Hardcoded these parameters here, but these parameters still won't work in models
// Just try to better recover the original motion3.json
paramDb.Add("PARAM_ARM_R_10");
paramDb.Add("PARAM_ARM_L_10");

Console.WriteLine("Recovering parameters and looking up for assets...");
foreach (var resource in resources)
{
    manager.LoadFiles(resource);

    if (manager.assetsFileList.Count == 0)
    {
        continue;
    }
    
    foreach (var asset in manager.assetsFileList.First().Objects)
    {
        switch (asset)
        {
            case TextAsset m_TextAsset:
                if (m_TextAsset.m_Name.Contains(".moc3"))
                {
                    var asciiStr = Encoding.ASCII.GetString(m_TextAsset.m_Script);
                
                    var matches = Regex.Matches(asciiStr, "\0(Param[a-zA-Z0-9-_]*)\0", RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        var name = match.Groups[1].Value;
                        paramDb.Add(name);

                        // Again, Some motion asset uses parameters in wrong forms (CamelCase or UnderscoreCase)
                        // Even if we recover these parameters, they still won't work in models
                        // Just try to better recover the original motion3.json

                        if (name.StartsWith("Param")) paramDb.Add(match.Groups[1].Value.ToUnderscoreCase());
                        if (name.StartsWith("PARAM")) paramDb.Add(match.Groups[1].Value.ToCamelCase());
                    }
                }

                break;
            case AssetBundle m_AssetBundle:
                foreach (var m_Container in m_AssetBundle.m_Container)
                {
                    var preloadIndex = m_Container.Value.preloadIndex;
                    var preloadSize = m_Container.Value.preloadSize;
                    var preloadEnd = preloadIndex + preloadSize;
                    for (int k = preloadIndex; k < preloadEnd; k++)
                    {
                        var pptr = m_AssetBundle.m_PreloadTable[k];
                        if (pptr.TryGet(out var obj))
                        {
                            containers[obj] = m_Container.Key;
                        }
                    }
                }
                break;
        }
    }
    
    manager.Clear();
}

paramDb = paramDb.Distinct().ToList();
partsDb = partsDb.Distinct().ToList();

Console.WriteLine("Exporting models...");

foreach (var container in containers)
{
    var asset = container.Key;
    var containerPath = container.Value;

    if (asset is AnimationClip mAnimationClip)
    {
        var clips = new List<AnimationClip>();
        clips.Add(mAnimationClip);

        var converter = new CubismMotion3Converter(paramDb, partsDb, clips.ToArray());
        foreach (var animation in converter.AnimationList)
        {
            var elementPath = containerPath.Substring(0, containerPath.LastIndexOf("/"));
            var outputPath = Path.Combine(outputFolder, elementPath);
            var fileOutputPath = $"{outputPath}/{animation.Name}.motion3.json";

            var json = new CubismMotion3Json
            {
                Version = 3,
                Meta = new CubismMotion3Json.SerializableMeta
                {
                    Duration = animation.Duration,
                    Fps = animation.SampleRate,
                    Loop = true,
                    AreBeziersRestricted = true,
                    CurveCount = animation.TrackList.Count,
                    UserDataCount = animation.Events.Count
                },
                Curves = new CubismMotion3Json.SerializableCurve[animation.TrackList.Count]
            };

            var totalSegmentCount = 1;
            var totalPointCount = 1;

            for (var i = 0; i < animation.TrackList.Count; i++)
            {
                var track = animation.TrackList[i];
                json.Curves[i] = new CubismMotion3Json.SerializableCurve
                {
                    Target = track.Target,
                    Id = track.Name,
                    Segments = new List<float> { 0f, track.Curve[0].value }
                };

                for (var j = 1; j < track.Curve.Count; j++)
                {
                    var curve = track.Curve[j];
                    var preCurve = track.Curve[j - 1];
                    if (Math.Abs(curve.time - preCurve.time - 0.01f) < 0.0001f) //InverseSteppedSegment
                    {
                        var nextCurve = track.Curve[j + 1];
                        if (nextCurve.value == curve.value)
                        {
                            json.Curves[i].Segments.Add(3f);
                            json.Curves[i].Segments.Add(nextCurve.time);
                            json.Curves[i].Segments.Add(nextCurve.value);
                            j += 1;
                            totalPointCount += 1;
                            totalSegmentCount++;
                            continue;
                        }
                    }

                    if (float.IsPositiveInfinity(curve.inSlope)) //SteppedSegment
                    {
                        json.Curves[i].Segments.Add(2f);
                        json.Curves[i].Segments.Add(curve.time);
                        json.Curves[i].Segments.Add(curve.value);
                        totalPointCount += 1;
                    }
                    else if (preCurve.outSlope == 0f && Math.Abs(curve.inSlope) < 0.0001f) //LinearSegment
                    {
                        json.Curves[i].Segments.Add(0f);
                        json.Curves[i].Segments.Add(curve.time);
                        json.Curves[i].Segments.Add(curve.value);
                        totalPointCount += 1;
                    }
                    else //BezierSegment
                    {
                        var tangentLength = (curve.time - preCurve.time) / 3f;
                        json.Curves[i].Segments.Add(1f);
                        json.Curves[i].Segments.Add(preCurve.time + tangentLength);
                        json.Curves[i].Segments.Add(preCurve.outSlope * tangentLength + preCurve.value);
                        json.Curves[i].Segments.Add(curve.time - tangentLength);
                        json.Curves[i].Segments.Add(curve.value - curve.inSlope * tangentLength);
                        json.Curves[i].Segments.Add(curve.time);
                        json.Curves[i].Segments.Add(curve.value);
                        totalPointCount += 3;
                    }

                    totalSegmentCount++;
                }
            }

            json.Meta.TotalSegmentCount = totalSegmentCount;
            json.Meta.TotalPointCount = totalPointCount;

            json.UserData = new CubismMotion3Json.SerializableUserData[animation.Events.Count];
            var totalUserDataSize = 0;

            for (var i = 0; i < animation.Events.Count; i++)
            {
                var @event = animation.Events[i];
                json.UserData[i] = new CubismMotion3Json.SerializableUserData
                {
                    Time = @event.time,
                    Value = @event.value
                };
                totalUserDataSize += @event.value.Length;
            }

            json.Meta.TotalUserDataSize = totalUserDataSize;

            var jsonSerialized = JsonConvert
                .SerializeObject(json, Formatting.Indented, new CubismExportedJson());

            Directory.CreateDirectory(outputPath);
            File.WriteAllText(fileOutputPath, jsonSerialized);
        }
    }
}

Console.WriteLine($"Exported total {containers.Count} animations.");
