"""Checks project structure/metadata only. This does not compile or execute Unity."""
import json
import pathlib
import re
import sys

root = pathlib.Path(__file__).resolve().parents[1]
errors = []
guids = {}
for path in (root / "Assets").rglob("*"):
    if path.suffix == ".meta":
        matches = re.findall(r"^guid: ([0-9a-f]{32})$", path.read_text(), re.M)
        if len(matches) != 1:
            errors.append(f"Invalid meta GUID: {path.relative_to(root)}")
        elif matches[0] in guids:
            errors.append(f"Duplicate GUID: {path.relative_to(root)}")
        else:
            guids[matches[0]] = path
    elif not pathlib.Path(str(path) + ".meta").exists():
        errors.append(f"Missing meta: {path.relative_to(root)}")
    if path.suffix in {".json", ".asmdef"}:
        json.loads(path.read_text())

manifest = json.loads((root / "Packages/manifest.json").read_text())
assert manifest["dependencies"]["com.unity.render-pipelines.universal"] == "17.3.0"
assert manifest["dependencies"]["com.unity.inputsystem"] == "1.17.0"
for path in (root / "Assets/RushArena/Core").rglob("*.cs"):
    core = path.read_text()
    if "using UnityEngine" in core or "using Fusion" in core:
        errors.append(f"Engine/network dependency in pure Core: {path.name}")
if errors:
    print("\n".join(errors), file=sys.stderr)
    sys.exit(1)
print(f"PASS structure, manifest, pure-core boundaries and {len(guids)} unique asset GUIDs.")
print("This is NOT Unity compilation or runtime/device verification.")
