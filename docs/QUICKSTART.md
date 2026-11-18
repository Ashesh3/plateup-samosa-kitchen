# Samosa Kitchen 1.1.2 — Masala Counter

**Recipe difficulty: 5/5 stars.** This update changes the recipe-card rating only; cook times, customer count, rewards and the 15 filling combinations are unchanged.

One main, four scoops, fifteen orders. Every customer asks for one potato samosa with a nonempty selection of distinct masalas. A four-symbol ticket means **all four in the same pastry**, not four separate samosas.

| Cycle order | Scoop | Symbol |
|---|---|---|
| 1 · G | Green coriander-pea | Leaf |
| 2 · R | Red chilli | Chilli pepper |
| 3 · Y | Yellow turmeric | Sun |
| 4 · B | Brown roasted cumin | Three seeds |

**At the counter:** tap **Interact** to advance one bowl. Use **Grab** to take one scoop; grabbing doesn't advance the selection. A cream-and-dark outline frames the active bowl and a raised sign repeats its symbol. Each station tracks its own selection. Match the same symbols on the order and the food, without relying on colour.

**At the hob:**
1. Chop a potato. Add each requested scoop once, in any order. Keep the filling cold until you have the full mix. Extra or missing flavors won't satisfy the order; duplicate scoops don't merge.
2. Cook the mixed filling on a hob. Knead flour into dough while it cooks.
3. Add the cooked filling to the dough. Knead the open parcel to seal it.
4. Fry until golden, then put **one** samosa on a clean plate. Both filling and fried pastry can burn.

The four ingredients/equipment requests are still flour, potatoes, **one** masala counter and clean plates. The green recipe retains its 1.0.x item IDs. Orders use one native menu item with fifteen alternatives: four singles, six pairs, four triples and one all-four; groups of a given size are not equally likely as classes.

The first counter is a required ingredient source when Samosa is on the menu; the game places it in an ingredient slot or sends an ingredient parcel if no slot is free. The random shop may offer another after you own one. It won't roll it as a first purchase in an unrelated restaurant. The physical counter blocks players like a normal ingredient crate; prep-phase NoClip mods can intentionally override that.

**Install:** extract the mod ZIP beside `PlateUp.exe` and replace `Mods/SamosaKitchen/content/SamosaKitchen.dll`. Restart the game. Use the same version on host and guests. Built for PlateUp! 1.5.3 with KitchenLib 0.10.2. No additional flavor-specific dependencies.

Existing Samosa restaurants can start receiving mixed orders after upgrading. Don't downgrade a save containing the new fillings to 1.0.x; restore a backup from before the flavor update instead.

**Development:** run `./build.ps1 -Game "path/to/PlateUp"`. The DLL embeds its original models. Use `python tools/make_art.py --obj Art/obj` for editable models, `python tools/make_variants.py` for the fifteen explicit type registrations and `python -m unittest discover -s tests` for offline tests. `tests/SamosaProbe.cs` is a development-only in-engine probe; do not install it for normal play.
