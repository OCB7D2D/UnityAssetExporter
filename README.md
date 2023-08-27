# Unity3D Resource Bundle Multi-Asset Exporter

This package adds two different way to create `unity3d` resource
bundles. It contains the well known asset exporter script known
by the 7 Days to Die modding scene, with different options to
make use of the much faster LZ4 compression more easily.

![Exporter Script options](.images/unity-exporter-script-options.png)

And an additional more advanced way to handle `unity3d` resource
bundles, by defining all assets that go into the resource bundle
once. You can there also configure various options, e.g. to use
either LZ4 or LZMA compression, and you only define the save
location once. Then you just update the export `unity3d`
bund file on demand by clicking a single button.

![Create Unity3D Bundle Asset](.images/unity-bundle-3d-create.png)

## Use in Unity

Use the following url to add it to unity via package manager:

https://github.com/OCB7D2D/UnityAssetExporter.git#upm@master

![Unity Package Manager](.images/unity-package-manager.png)

### Use specific version

You may also use a specific version by specifying a release:

https://github.com/OCB7D2D/UnityAssetExporter.git#upm@0.6.5

See https://github.com/OCB7D2D/UnityAssetExporter/branches

### Add as dependency to your unity project

If you want to add this exporter as a dependency to your project,
you can either do that by using the package manager, or you could
also just edit/create `Packages/manifest.json` with the following:

```json
{
  "dependencies": {
    "ch.ocbnet.unityassetexporter": "https://github.com/OCB7D2D/UnityAssetExporter.git#upm@master",
  }
}
```

## Unity3D Asset Bundle Exporter

Once you have created the asset in any folder, you can start to
configure it. You mainly need to select or drag&drop all assets
you want to export into the `Objects` list.

Then "Set unity3d resource path" to where the final bundle should
be stored. Latest version will make this path relative to make it
easier to copy your unity project around (at least for me).

![Selected Assets in Bundle3D](.images/unity-bundle-3d-assets.png)

Alternatively you can enable the incubating feature to just include
everything under certain folders. For that you need to enable the
checkmark "Include assets from folders". Now you can also add
folders directly to the "Objects" list and everything in it will
be included in the export (fine-tune by toggling recursiveness).

![Selected Assets in Bundle3D](.images/unity-bundle-3d-folders.png)


## Changelog

### Version 0.7.0

- Add platform specific bundle options
- May address MacOSX issues (e.g. shaders)
- Use our custom UPM packaging action

### Version 0.6.5

- Fix deployment issue and version
- Fix default filename for old script exporter

### Version 0.6.4

- Fix folder includes to not start from parent folder

### Version 0.6.3

- Fix issue when changing options without game loaded

### Version 0.6.2

- Add old exporter script with different compression options

### Version 0.6.0

- Initial version