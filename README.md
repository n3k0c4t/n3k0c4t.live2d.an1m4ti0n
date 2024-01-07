# n3k0c4t.live2d.an1m4ti0n

一个针对**特定游戏** Live2D Motion 资源的提取工具。

**WARNING: HIGH MEMORY USAGE**

通过读取 `.moc3` 获得参数列表（方法比较 Dirty），提取 `.anim` 文件、绑定参数并转换回 `.motion3.json`。Motion 以外的其他资源需自行提取。

基于 [UnityLive2DExtractor](https://github.com/Perfare/UnityLive2DExtractor) 修改而成，版权归原作者所有。

## Command-line
n3k0c4t.live2d.an1m4ti0n.exe <asset map> <output folder>

## Requirements
- [.NET 6](https://dotnet.microsoft.com/ja-jp/download/dotnet/6.0)

## Thanks

- [UnityCNLive2DExtractor](https://github.com/Razmoth/UnityCNLive2DExtractor)
- [SEKAI2DMotionExtractor](https://github.com/Coxxs/SEKAI2DMotionExtractor)
- [AssetStudio](https://github.com/RazTools/Studio)
- [UnityLive2DExtractor](https://github.com/Perfare/UnityLive2DExtractor)
