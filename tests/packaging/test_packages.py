import pathlib
import runpy
import tarfile
import tempfile
import unittest
import zipfile

REPOSITORY = pathlib.Path(__file__).resolve().parents[2]


class PackageTests(unittest.TestCase):
    def test_tar_and_marketplace_zip_contain_the_same_nested_assets(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            source = root / "SubtitleEditPlugin" / "Release"
            for name, data in {
                "bin/SubtitleEditPlugin.dll": b"test-dll",
                "metadata/Icon256x256.png": b"icon",
                "metadata/LoupedeckPackage.yaml": b"version: 1.3.1\n",
                "actionsymbols/nested/button.svg": b"<svg/>",
                "localization/SubtitleEdit_pt-PT.xliff": b"<xliff/>",
            }.items():
                dst = source / name
                dst.parent.mkdir(parents=True, exist_ok=True)
                dst.write_bytes(data)
            version = root / "SubtitleEditPlugin" / "package" / "metadata" / "LoupedeckPackage.yaml"
            version.parent.mkdir(parents=True, exist_ok=True)
            version.write_text("version: 1.3.1\n", encoding="utf-8")

            tar_functions = runpy.run_path(
                str(REPOSITORY / "scripts" / "pack-lplug4.py"), run_name="packer")
            zip_functions = runpy.run_path(
                str(REPOSITORY / "scripts" / "pack-lplug4-zip.py"), run_name="zipper")
            for functions in (tar_functions, zip_functions):
                functions["main"].__globals__["SOURCE"] = source
                functions["main"].__globals__["ROOT"] = root
            tar_functions["main"].__globals__["OUT"] = root / "dist" / "SubtitleEdit_1_3_1.lplug4"
            tar_functions["main"]()
            zip_functions["main"]()
            tarpath = root / "dist" / "SubtitleEdit_1_3_1.lplug4"
            zippath = root / "dist" / "SubtitleEdit_1_3_1_marketplace.lplug4"
            with tarfile.open(tarpath, "r:") as tar, zipfile.ZipFile(zippath) as zf:
                tar_files = {i.name: tar.extractfile(i).read()
                             for i in tar.getmembers() if i.isfile()}
                zip_files = {n: zf.read(n) for n in zf.namelist()}
                self.assertEqual(tar_files, zip_files)
                self.assertIn("actionsymbols/nested/button.svg", tar_files)
                self.assertTrue(all((i.mode & 0o022) == 0 for i in tar.getmembers()))
                self.assertNotIn("bin/PluginApi.dll", tar_files)


if __name__ == "__main__":
    unittest.main()
