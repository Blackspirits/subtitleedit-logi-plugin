#!/usr/bin/env python3
"""
Packs the built plugin into a .lplug4 package.

A .lplug4 is an uncompressed POSIX ustar archive whose root holds bin/ and
metadata/, with directory entries present. The SDK documentation describes it as
"essentially a zip file", which it is not: a zip is silently rejected by the
plugin service, and double clicking it appears to do nothing. This was confirmed
by unpacking a published Logitech plugin.

Usage:  python3 scripts/pack-lplug4.py [Release|Debug]
"""
import io
import pathlib
import sys
import tarfile

CONFIG = sys.argv[1] if len(sys.argv) > 1 else "Release"
ROOT = pathlib.Path(__file__).resolve().parent.parent
SOURCE = ROOT / "SubtitleEditPlugin" / CONFIG
def _version():
    import re
    y = (ROOT / "SubtitleEditPlugin" / "package" / "metadata" / "LoupedeckPackage.yaml").read_text()
    m = re.search(r"^version:\s*(\S+)", y, re.M)
    return (m.group(1) if m else "1_0").replace(".", "_")

OUT = ROOT / "dist" / f"SubtitleEdit_{_version()}.lplug4"
MTIME = 1767974400  # fixed, so the package is reproducible


def entry(name, mode, size, kind):
    info = tarfile.TarInfo(name)
    info.mode, info.uid, info.gid = mode, 0, 0
    info.uname, info.gname = "", ""
    info.mtime, info.size, info.type = MTIME, size, kind
    return info


def main():
    if not (SOURCE / "bin").is_dir():
        sys.exit(f"No build found at {SOURCE}. Run: dotnet build -c {CONFIG}")

    # localization.generated is the service's scratch output, not part of the package.
    folders = [d.name for d in sorted(SOURCE.iterdir())
               if d.is_dir() and d.name != "localization.generated"]
    if "bin" not in folders or "metadata" not in folders:
        sys.exit(f"Expected bin/ and metadata/ under {SOURCE}, found {folders}")

    OUT.parent.mkdir(exist_ok=True)
    with tarfile.open(OUT, "w", format=tarfile.USTAR_FORMAT) as tar:
        for folder in folders:
            tar.addfile(entry(folder + "/", 0o755, 0, tarfile.DIRTYPE))
        for folder in folders:
            root = SOURCE / folder
            for path in sorted(root.rglob("*")):
                name = path.relative_to(SOURCE).as_posix()
                if path.is_dir():
                    tar.addfile(entry(name + "/", 0o755, 0, tarfile.DIRTYPE))
                elif path.is_file():
                    data = path.read_bytes()
                    tar.addfile(entry(name, 0o644, len(data), tarfile.REGTYPE),
                                io.BytesIO(data))

    names = tarfile.open(OUT).getnames()
    for required in ("metadata/LoupedeckPackage.yaml", "metadata/Icon256x256.png",
                     "bin/SubtitleEditPlugin.dll"):
        if required not in names:
            sys.exit(f"Package is missing {required}")

    print(f"wrote {OUT} ({OUT.stat().st_size / 1048576:.1f} MB, {len(names)} entries)")


if __name__ == "__main__":
    main()
