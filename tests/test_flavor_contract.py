import re
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]

class FlavorContract(unittest.TestCase):
    def test_explicit_closed_registration_has_every_mix_exactly_once(self):
        source=(ROOT/"src"/"FlavorVariants.g.cs").read_text()
        observed=[int(s) for s in re.findall(r'RegisterMask<M(\d{2})>\(mod\);',source)]
        self.assertEqual(observed,list(range(1,16)))
        masks=[(int(a),int(b)) for a,b in re.findall(r'(?:class|struct) M(\d{2}) : IMask \{ public int Bits => (\d+);',source)]
        self.assertEqual(masks,[(x,x) for x in range(1,16)])
        self.assertEqual([sum(m.bit_count()==i for m in observed) for i in range(1,5)],[4,6,4,1])
