"""Build and atomically install Plant Library, Zombie Library and Plant Fusion without starting the game."""
import argparse
from datetime import datetime, timezone
import fcntl
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
GAME = Path("/mnt/FastGaming1/SteamLibrary/steamapps/common/PVZ Replanted")
DOTNET = ROOT.parent / ".tools/dotnet/dotnet"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def atomic(path, data):
    fd, temporary = tempfile.mkstemp(dir=path.parent, prefix=".PlantLibrary-")
    try:
        with os.fdopen(fd, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, 0o644)
        os.replace(temporary, path)
        directory = os.open(path.parent, os.O_DIRECTORY)
        try:
            os.fsync(directory)
        finally:
            os.close(directory)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def install(game, example, build, zombie_example=False, backup_root=None):
    fusion = ROOT.parent / "fusion-mod"
    zombie = ROOT.parent / "zombie-library"
    backup_root = backup_root or ROOT
    sources = {"PlantLibrary.dll": ROOT / "bin/Release/net6.0/PlantLibrary.dll",
               "FusionMod.dll": fusion / "bin/Release/net6.0/FusionMod.dll",
               "ZombieLibrary.dll": zombie / "bin/Release/net6.0/ZombieLibrary.dll"}
    if example:
        sources["PlantLibraryExample.dll"] = ROOT / "example-mod/bin/Release/net6.0/PlantLibraryExample.dll"
    if zombie_example:
        sources["ZombieLibraryExample.dll"] = zombie / "example-mod/bin/Release/net6.0/ZombieLibraryExample.dll"
    originals = {name: (game / "Mods" / name).read_bytes() if (game / "Mods" / name).exists() else None
                 for name in sources}
    if build:
        subprocess.run([str(DOTNET), "build", str(fusion / "FusionMod.csproj"), "-c", "Release",
                        "--nologo", f"-p:GameDir={game}"], check=True)
        if example:
            subprocess.run([str(DOTNET), "build", str(ROOT / "example-mod/PlantLibraryExample.csproj"),
                            "-c", "Release", "--nologo", f"-p:GameDir={game}"], check=True)
        if zombie_example:
            subprocess.run([str(DOTNET), "build", str(zombie / "example-mod/ZombieLibraryExample.csproj"),
                            "-c", "Release", "--nologo", f"-p:GameDir={game}"], check=True)
    payloads = {name: path.read_bytes() for name, path in sources.items()}
    backup = backup_root / "backups" / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    backup.mkdir(parents=True)
    for name, data in originals.items():
        if data is not None:
            atomic(backup / name, data)
            if (backup / name).read_bytes() != data:
                raise RuntimeError("Backup verification failed: " + name)
    deployed = []
    try:
        for name, data in payloads.items():
            target = game / "Mods" / name
            current = target.read_bytes() if target.exists() else None
            if current != originals[name]:
                raise RuntimeError("Installed DLL changed during build: " + name)
            deployed.append(name)
            atomic(target, data)
            if target.read_bytes() != data:
                raise RuntimeError("Installation verification failed: " + name)
    except BaseException:
        for name in reversed(deployed):
            target = game / "Mods" / name
            if originals[name] is None:
                target.unlink(missing_ok=True)
            else:
                atomic(target, originals[name])
        raise
    proof = {"installed": {name: digest(data) for name, data in payloads.items()},
             "previous": {name: digest(data) if data is not None else None for name, data in originals.items()},
             "backup": str(backup), "game_started": False, "tests_run": False}
    atomic(backup / "installation.json", (json.dumps(proof, indent=2) + "\n").encode())
    checksums = "".join(f"{digest(data)}  {name}\n" for name, data in payloads.items())
    atomic(ROOT / "SHA256SUMS", checksums.encode())
    atomic(zombie / "SHA256SUMS", checksums.encode())
    fusion_checksums = "".join(f"{digest((game / 'Mods' / name).read_bytes())}  {name}\n"
                               for name in ("LawnGlove.dll", "PlantLibrary.dll", "ZombieLibrary.dll", "FusionMod.dll"))
    atomic(fusion / "SHA256SUMS", fusion_checksums.encode())
    print("Installed: " + ", ".join(payloads))
    print("Backup: " + str(backup))
    print("Game and test programs were not started.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-dir", type=Path, default=GAME)
    parser.add_argument("--example", action="store_true", help="Also build and install the example mod")
    parser.add_argument("--no-build", action="store_true", help="Install already compiled Release artifacts")
    args = parser.parse_args()
    with (args.game_dir / "Mods/.FusionMod.release.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        install(args.game_dir, args.example, not args.no_build)
