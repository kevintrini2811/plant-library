"""Export and compose native menu artwork without loading the game runtime."""
import hashlib
import json
from pathlib import Path
import UnityPy
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
GAME = Path("/mnt/FastGaming1/SteamLibrary/steamapps/common/PVZ Replanted")
BUNDLES = GAME / "Replanted_Data/StreamingAssets/aa/StandaloneWindows64"
BUNDLE = BUNDLES / "backgroundatlases_assets_all_19e402bc532d2cf4f6d062dcf0b108ec.bundle"
PEASHOOTER_BUNDLE = BUNDLES / "defaultlocalgroup_assets_assets/art/characters/previews_f999564bc5382eb85495ec50c887a03d.bundle"
PATH_ID = -5079667051938085663
PEASHOOTER_ID = 3333466314890536487
CROP = (1100, 600, 4000, 1800)
SIZE = (1450, 600)
ICON_SIZE = (512, 512)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export():
    environment = UnityPy.load(str(BUNDLE), str(PEASHOOTER_BUNDLE))
    native = next(obj.read() for obj in environment.objects if obj.type.name == "Sprite" and obj.path_id == PATH_ID)
    pea = next(obj.read() for obj in environment.objects if obj.type.name == "Sprite" and obj.path_id == PEASHOOTER_ID)
    if native.m_Name != "Area1_Lawn" or pea.m_Name != "SPR_Preview_PeaShooter":
        raise ValueError("Unexpected native menu artwork")
    ROOT.joinpath("assets").mkdir(exist_ok=True)
    lawn = native.image.convert("RGBA").crop(CROP).resize(SIZE, Image.Resampling.LANCZOS)
    lawn.save(ROOT / "assets/lawn.png")
    portrait = pea.image.convert("RGBA")
    portrait.save(ROOT / "assets/peashooter.png")
    # BloomEngine displays one square icon inside its own native border.
    # Compose the background into that icon instead of modifying the text row.
    icon = ImageOps.fit(lawn, ICON_SIZE, method=Image.Resampling.LANCZOS)
    icon.putalpha(255)
    foreground = ImageOps.contain(portrait, (440, 440), method=Image.Resampling.LANCZOS)
    icon.alpha_composite(foreground, ((512 - foreground.width) // 2, 512 - foreground.height - 28))
    icon.save(ROOT / "assets/menu-icon.png")
    proof = {"bundle": str(BUNDLE), "bundle_sha256": sha256(BUNDLE), "sprite": native.m_Name,
             "path_id": PATH_ID, "crop": CROP, "size": SIZE,
             "icon_bundle": str(PEASHOOTER_BUNDLE), "icon_bundle_sha256": sha256(PEASHOOTER_BUNDLE),
             "icon_sprite": pea.m_Name, "icon_path_id": PEASHOOTER_ID,
             "icon_size": ICON_SIZE, "foreground_bounds": [440, 440], "foreground_bottom": 28,
             "assets": {name: sha256(ROOT / "assets" / name)
                        for name in ("lawn.png", "peashooter.png", "menu-icon.png")}}
    ROOT.joinpath("assets/sources.json").write_text(json.dumps(proof, indent=2) + "\n")
    print("Exported native Peashooter cutout and opaque lawn icon.")


if __name__ == "__main__":
    export()
