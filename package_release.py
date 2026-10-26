"""Package a versioned GitHub release from an explicitly built desktop binary."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def archive_folder(folder, destination):
    with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(folder.rglob('*')):
            if path.is_file():
                archive.write(path, folder.name + '/' + path.relative_to(folder).as_posix())
    with zipfile.ZipFile(destination) as archive:
        bad = archive.testzip()
        if bad is not None:
            raise RuntimeError('Archive CRC mismatch: ' + bad)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--binary-dir', type=Path, default=ROOT / 'release')
    parser.add_argument('--output-dir', type=Path, default=ROOT / 'dist')
    args = parser.parse_args()
    version = json.loads((ROOT / 'version.json').read_text(encoding='utf-8'))
    tag = 'v' + version['version']
    prefix = version['name'] + '-' + tag
    binary_dir = args.binary_dir.resolve()
    executable = binary_dir / 'DefectCompanion.exe'
    if not executable.is_file():
        raise SystemExit('Build the application first with build.ps1')
    import subprocess
    escaped_exe = str(executable).replace("'", "''")
    metadata = subprocess.run(['powershell.exe', '-NoProfile', '-NonInteractive', '-Command',
        "(Get-Item -LiteralPath '" + escaped_exe + "').VersionInfo | Select-Object ProductVersion, FileVersion | ConvertTo-Json -Compress"],
        capture_output=True, text=True, check=True)
    actual = json.loads(metadata.stdout)
    if actual['ProductVersion'] != version['version'] or actual['FileVersion'] != version['assemblyVersion']:
        raise SystemExit('Binary version does not match version.json; rebuild before packaging')
    output = args.output_dir.resolve()
    output.mkdir(parents=True, exist_ok=True)
    portable = output / (prefix + '-windows-portable')
    source = output / (prefix + '-source')
    if portable.exists() or source.exists():
        raise SystemExit('Package staging folder already exists; use a new output directory')
    portable.mkdir()
    source.mkdir()
    for name in ['DefectCompanion.exe', 'DefectCompanion.exe.config']:
        shutil.copy2(binary_dir / name, portable / name)
    shutil.copy2(ROOT / 'README.md', portable / '使用说明.md')
    for name in ['CHANGELOG.md', 'THIRD_PARTY_NOTICES.md', 'version.json']:
        shutil.copy2(ROOT / name, portable / name)
    allowed = {'.cs', '.py', '.ps1', '.md'}
    named = {'version.json', 'card_catalog.json', 'art-direction.json',
             'App.config', 'app.manifest', 'pet.ico', 'pet.png', '.gitignore'}
    for path in sorted(ROOT.iterdir()):
        if path.is_file() and (path.suffix in allowed or path.name in named):
            if path.name != 'Version.Generated.cs':
                shutil.copy2(path, source / path.name)
    (source / 'assets').mkdir()
    for path in sorted((ROOT / 'assets').iterdir()):
        if path.is_file() and (path.suffix in {'.png', '.json'} or path.name == 'mod-cable-prefabs.bin'):
            if not path.name.startswith('motion'):
                shutil.copy2(path, source / 'assets' / path.name)
    for folder, suffixes in [('docs', {'.md'}), ('mod-study', {'.md', '.json'}),
                              ('native-study', {'.gd', '.godot', '.gdextension', '.md'})]:
        for path in sorted((ROOT / folder).glob('*')):
            if path.is_file() and path.suffix in suffixes:
                target = source / folder / path.name
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(path, target)
    files = []
    for folder in [portable, source]:
        target = output / (folder.name + '.zip')
        archive_folder(folder, target)
        files.append({'name': target.name, 'bytes': target.stat().st_size, 'sha256': digest(target)})
    (output / 'SHA256SUMS.txt').write_text(''.join(item['sha256'] + '  ' + item['name'] + '\n' for item in files), encoding='utf-8')
    report = {'version': version['version'], 'tag': tag, 'channel': version['channel'],
              'internal_baseline': 'v23', 'binary_sha256': digest(executable),
              'archives_verified': True, 'files': files}
    (output / 'release-manifest.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    (output / 'RELEASE_NOTES.md').write_text((ROOT / 'CHANGELOG.md').read_text(encoding='utf-8') +
        '\n下载 `'+prefix+'-windows-portable.zip`，解压后运行 `DefectCompanion.exe`。源码包用于修改和编译。\n', encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))

if __name__ == '__main__':
    main()
