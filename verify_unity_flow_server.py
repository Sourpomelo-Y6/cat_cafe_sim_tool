"""Run isolated HTTP fixtures for Unity live UI verification; never open user saves."""
import argparse
import sys
import tempfile
import threading
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "cat_cafe_sim"))
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions
from cat_cafe_sim.unity_state_server import make_server, state_view


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=("management", "goals", "ended"))
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    stop = args.output / (args.mode + "-stop.txt")
    if stop.exists():
        raise RuntimeError("Use a fresh output directory for each run")
    with tempfile.TemporaryDirectory(prefix="cat-cafe-ui-live-") as directory:
        settings = starting_conditions("popularity" if args.mode == "goals" else "free")
        settings["store_events"]["probability"] = 0
        for key in ("intake_request", "reservation", "growth", "regular_introduction", "visiting_cat"):
            settings.pop(key, None)
        settings["management"]["starting_funds"] = 1 if args.mode == "ended" else 10000
        if args.mode == "goals":
            settings["goal"].update(target=105, days=10, stages=[dict(target=110, days=12), dict(target=115, days=14)])
        session = create_game(Path(directory) / "source", settings)
        if args.mode == "goals":
            while not session.core.closed:
                session.automatic_step()
            assert state_view(session)["goal_result"]["status"] == "cleared"
        elif args.mode == "ended":
            session.day_off()
            assert state_view(session)["game_over"] is not None
        else:
            for cat in session.core.cats.values():
                session.core.management["stress"][cat.id] = 20
                cat.fatigue = 35
        originals = {path: path.read_bytes() for path in session.checkpoint_path.parent.iterdir() if path.is_file()}
        with make_server(session, 0, Path(directory) / "saves") as server:
            (args.output / (args.mode + "-port.txt")).write_text(str(server.server_port), encoding="utf-8")
            print("UI_LIVE_SERVER_READY:" + args.mode, flush=True)
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            try:
                deadline = time.monotonic() + 1200
                while time.monotonic() < deadline and not stop.exists():
                    time.sleep(0.2)
            finally:
                server.shutdown()
                thread.join()
        assert all(path.read_bytes() == data for path, data in originals.items())
        print("UI_LIVE_ORIGINAL_FILES_UNCHANGED:" + args.mode, flush=True)


if __name__ == "__main__":
    main()
