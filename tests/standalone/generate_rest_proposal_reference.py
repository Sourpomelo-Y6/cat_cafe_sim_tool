"""Python UI references; no server, game saves confined to an isolated temporary directory."""
import argparse
import itertools
import json
import sys
import tempfile
from pathlib import Path
from types import SimpleNamespace

parser = argparse.ArgumentParser()
parser.add_argument("python_repository", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
sys.path.insert(0, str(args.python_repository.resolve()))
from cat_cafe_sim.core.cafe_shifts import fatigue_rest_schedule

ids = ["cat-kohaku", "cat-mike", "cat-mugi", "cat-sora", "cat-tama"]
cases = []
for fatigue, eligible in itertools.product([(0, 0, 0, 0, 0), (30, 10, 30, 0, 90), (100, 100, 20, 20, 0)], range(32)):
    rows = [dict(id=id, fatigue=fatigue[i], health="healthy" if eligible & (1 << i) else "sick", activity="cafe") for i, id in enumerate(ids)]
    cats = {r["id"]: SimpleNamespace(fatigue=r["fatigue"], health_status=r["health"]) for r in rows if r["activity"] == "cafe"}
    cases.append(dict(cats=rows, expected=fatigue_rest_schedule(cats)))
for activity in ["dispatched", "missing", "adopted"]:
    rows = [dict(id=id, fatigue=i*20, health="healthy", activity=activity if i%2 else "cafe") for i,id in enumerate(ids)]
    cats = {r["id"]: SimpleNamespace(fatigue=r["fatigue"], health_status=r["health"]) for r in rows if r["activity"] == "cafe"}
    cases.append(dict(cats=rows, expected=fatigue_rest_schedule(cats)))
args.output.parent.mkdir(parents=True, exist_ok=True)
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions
with tempfile.TemporaryDirectory(dir=args.output.parent) as directory:
    game = create_game(Path(directory) / "game", starting_conditions("free"), seed=0)
    while not game.core.closed:
        game.automatic_step()
    summary = game.core.summary()
    day = dict(funds=summary["funds"], revenue=summary["revenue"], expenses=summary["operating_cost"], completed=summary["completed_interactions"], popularity=summary["popularity"],
               cats=[dict(id=id,stamina=cat.stamina,fatigue=cat.fatigue,stress=game.core.management["stress"][id]) for id,cat in sorted(game.core.cats.items())])
args.output.write_text(json.dumps(dict(cases=cases, day=day), ensure_ascii=False), encoding="utf-8")
print(f"Python rest proposal references: {len(cases)}")
