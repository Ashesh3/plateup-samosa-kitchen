import math
import struct
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPECTED = {"peas","mix","filling","open","raw","golden","plated","counter",
            "badge_g","badge_r","badge_y","badge_b","selector","scoop_r","scoop_y","scoop_b"}


def load_models():
    # Independent decoder for the format consumed by the Unity prefab factory.
    b = memoryview((ROOT / "Assets" / "samosa.smk").read_bytes())
    pos = 0

    def unpack(fmt):
        nonlocal pos
        out = struct.unpack_from("<" + fmt, b, pos)
        pos += struct.calcsize("<" + fmt)
        return out

    if bytes(unpack("4s")[0]) != b"SMK1":
        raise ValueError("unexpected mesh format")
    (n,) = unpack("H")
    palette = [unpack("3B") for _ in range(n)]
    models = {}
    for _ in range(unpack("H")[0]):
        name = bytes(unpack(str(unpack("B")[0]) + "s")[0]).decode("ascii")
        nv, nt = unpack("II")
        verts = [unpack("3f") for _ in range(nv)]
        faces = [unpack("3HB") for _ in range(nt)]
        if name in models:
            raise ValueError("duplicate model " + name)
        models[name] = verts, faces
    if pos != len(b):
        raise ValueError("trailing data")
    return palette, models


class Models(unittest.TestCase):
    def test_all_stages_present_and_not_empty(self):
        palette, models = load_models()
        self.assertEqual(set(models), EXPECTED)
        self.assertGreaterEqual(len(palette), 10)
        for name, (vs, fs) in models.items():
            with self.subTest(name=name):
                self.assertGreater(len(fs), 45)
                self.assertLess(len(fs), 3500)
                self.assertLess(len(vs), 65535)
                for a, b, c, mat in fs:
                    self.assertLess(mat, len(palette))
                    self.assertLess(max(a,b,c), len(vs))
                    u=tuple(x-y for x,y in zip(vs[b],vs[a]))
                    v=tuple(x-y for x,y in zip(vs[c],vs[a]))
                    normal=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
                    self.assertTrue(math.isfinite(sum(normal)))
                    self.assertGreater(sum(x*x for x in normal), 1e-17)
                self.assertLess(max(max(abs(x),abs(z)) for x,y,z in vs), .51)
                self.assertGreaterEqual(min(y for x,y,z in vs), -.006)

    def test_shaped_pocket_and_plated_readability(self):
        _, m=load_models()
        for k in ("raw","golden"):
            verts, faces=m[k]
            self.assertGreater(max(y for x,y,z in verts), .30)
            self.assertGreater(max(x for x,y,z in verts)-min(x for x,y,z in verts), .45)
        self.assertNotEqual(m["golden"],m["raw"])
        self.assertNotEqual(m["mix"],m["filling"])
        self.assertGreater(len(m["plated"][1]),len(m["golden"][1]))

    def test_four_symbols_have_distinct_geometry_as_well_as_colours(self):
        palette,m=load_models()
        icons=[m["badge_"+k] for k in "gryb"]
        self.assertEqual(len({tuple(v) for v,_ in icons}),4)
        self.assertEqual(len({frozenset(f[3] for f in faces) for _,faces in icons}),4)
        self.assertGreater(max(y for x,y,z in m["selector"][0]),.04)


if __name__ == "__main__":
    unittest.main()
