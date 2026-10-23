"""Summarize recorded C# runs and prior released Player UI evidence; never edits saves."""
import collections
import csv
import datetime
import hashlib
import json
import pathlib

WORK = pathlib.Path(__file__).resolve().parents[3]
RAW = WORK / "Logs/LongTermBalance"
OUT = WORK / "cat_cafe_sim_tool/Docs/Results/LongTermBalance"
OUT.mkdir(parents=True, exist_ok=True)


def read(p):
    return json.loads(p.read_text(encoding="utf-8-sig"))


def lines(p):
    return [json.loads(x) for x in p.read_text(encoding="utf-8-sig").splitlines()]


results = []
for group in ["final-initial", "final-manual", "probe"]:
    for r in read(RAW / group / "summary.json"):
        d = RAW / group / r["id"]
        s = read(d / "final.json")
        daily = lines(d / "daily.jsonl")
        opening = lines(d / "openings.jsonl")
        clear = (s["goal"][-1]["resolved_day"] if s["goal"] and s["goal"][-1]["status"] == "cleared"
                 else (s.get("bond_goal") or s.get("patron") or {}).get("resolved_day", 0))
        h = s["history"]
        after = [x for x in h if clear and x["day"] > clear]

        def share(history):
            counts = collections.Counter(x["cat_id"] for day in history for x in day["customer_records"]
                                         if x["cat_id"] and x["satisfaction"] is not None)
            return {"counts": dict(counts), "largest_share": max(counts.values(), default=0) / max(1, counts.total())}

        cats = [c for day in daily for c in day["cats"]]
        working = [c for day in opening for c in day["cats"] if c["working"]]
        r.update(clear_day=clear, recorded_closed_days=len(daily),
                 settled_days=len(h), unsampled_closed_days=[x["day"] for x in h if x["day"] not in {d["day"] for d in daily}],
                 funds_at_clear=next((x["funds"] for x in daily if x["day"] == clear), None),
                 spending={k:sum(x[k] for x in h) for k in ["pet_shop_expenses", "recruitment_expenses", "housing_expenses", "housing_cost", "expansion_expenses", "equipment_expenses", "rest_space_expenses", "waiting_area_expenses", "waiting_area_cost"]},
                 operating_expenses=sum(x["expenses"] for x in h),
                 net_change=s["funds"] - 1000,
                 minimum_closed_funds=min(x["funds"] for x in daily),
                 sick_closed_cat_days=sum(c["health"] == "sick" for c in cats),
                 missing_closed_cat_days=sum(c["activity"] == "missing" for c in cats),
                 zero_stamina_closed_cat_days=sum(c["stamina"] == 0 for c in cats),
                 mean_closed_fatigue=sum(c["fatigue"] for c in cats) / len(cats),
                 max_closed_fatigue=max(c["fatigue"] for c in cats),
                 mean_closed_stress=sum(c["stress"] for c in cats) / len(cats),
                 max_closed_stress=max(c["stress"] for c in cats),
                 working_mean_opening_stamina=sum(c["stamina"] for c in working) / max(1, len(working)),
                 working_mean_opening_deficit=sum(c["max"] - c["stamina"] for c in working) / max(1, len(working)),
                 working_low_openings=sum(c["stamina"] <= 20 for c in working),
                 post_clear=share(after), full_period=share(h),
                 missing_cases=len(s["missing_cases"]),
                 max_stamina_at_clear=max((c["max"] for day in daily if day["day"] == clear for c in day["cats"]), default=None),
                 player_sessions=len(s["player_sessions"]),
                 player_session_turns=dict(collections.Counter(x["set_ticks"] for x in s["player_sessions"])),
                 player_cat_affinity={x["cat_id"]: x["affinity"] for x in s["player_cats"]},
                 growth={g["cat_id"]: {k: g[k] for k in ["specialization", "selected_day", "service", "rest", "dispatch"]} for g in s["growth"]},
                 dispatch_by_destination=dict(collections.Counter(x["destination"] for x in s["dispatches"])),
                 strict_dispatches=[{k: x[k] for k in ["cat_id", "started_day", "resolved_day", "patron_match"]}
                                    for x in s["dispatches"] if x["destination"] == "patron_strict_visit"])
        results.append(r)

(OUT / "simulation.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8", newline="\n")
columns = ["id", "clear_day", "funds", "revenue", "operating_expenses", "minimum_closed_funds", "dispatches",
           "dispatch_reward", "sick_closed_cat_days", "missing_closed_cat_days", "zero_stamina_closed_cat_days",
           "mean_closed_fatigue", "max_closed_fatigue", "mean_closed_stress", "max_closed_stress",
           "working_mean_opening_stamina", "working_mean_opening_deficit", "working_low_openings", "max_stamina_at_clear"]
with (OUT / "comparison.csv").open("w", encoding="utf-8-sig", newline="") as f:
    writer = csv.DictWriter(f, columns, extrasaction="ignore", lineterminator="\n")
    writer.writeheader()
    writer.writerows(results)

# Historical Player records: emitted commands omit dialogue Next, which is counted in ui-actions.
ui = []
for route in ["completed/popularity", "standard/bond", "completed/patron"]:
    d = WORK / "Logs/IntegratedPlaytest" / route
    commands = lines(d / "commands.jsonl")
    commands = [dict(x, cmd=json.loads(x["command"])) for x in commands if x["command"].startswith("{")]
    # slot9 is the original ending checkpoint; later title/Extra sessions must not alter evidence.
    save = read(d / "saves/slot-9.json")
    events = []
    def scan(obj, path=""):
        if isinstance(obj, dict):
            if obj.get("conversation_id") and obj.get("completed"):
                events.append({"path": path, "template": obj["conversation_id"], "branch": obj.get("branch_id")})
            for k, v in obj.items():
                scan(v, path + "/" + k)
        elif isinstance(obj, list):
            for i, v in enumerate(obj):
                scan(v, path + "/" + str(i))
    # exclude state to avoid counting possible duplicate provenance as viewed conversations
    scan({k:v for k,v in save.items() if k != "state"})
    kinds = collections.Counter(x["cmd"]["kind"] for x in commands)
    stopped = []
    for i, x in enumerate(commands):
        if x["cmd"]["kind"] == "step_autoplay" and x["autoplay"] == "blocked" and (i == 0 or commands[i-1]["autoplay"] != "blocked"):
            stopped.append({"day": x["day"], "index": i})
    responses = [x for x in commands if x["cmd"]["kind"].startswith("resolve_") and "growth" not in x["cmd"]["kind"]]
    per_day = collections.Counter(x["day"] for x in responses)
    # UI log steps between the last event answer and resume/start, including visible page clicks.
    actions = []
    for line in (d / "ui-actions.txt").read_text(encoding="utf-8-sig").splitlines():
        time, _, action = line.partition(" ")
        actions.append((time, action))
    returns = []
    for i, x in enumerate(commands):
        if x["cmd"]["kind"] not in ["resume_autoplay", "start_autoplay"]:
            continue
        previous_steps = [j for j in range(i) if commands[j]["cmd"]["kind"] in ["step_autoplay", "start_autoplay", "resume_autoplay"]]
        if not previous_steps:
            continue
        start = previous_steps[-1]
        answered = [y for y in commands[start+1:i] if y["cmd"]["kind"].startswith("resolve_") and "growth" not in y["cmd"]["kind"]]
        if answered:
            last = answered[-1]
            visible = [a for t, a in actions if last["utc"] < t <= x["utc"]]
            returns.append({"day": x["day"], "kind": x["cmd"]["kind"], "commands_after_last_answer": i-commands.index(last),
                            "logged_ui_actions_after_answer": len(visible), "actions": visible})
    templates = collections.Counter(x["template"] for x in events)
    conversation_days = collections.defaultdict(list)
    state = save["state"]
    for e in save.get("equipment_conversations", []):
        if e["progress"]["completed"]:
            conversation_days[e["day"]].append(e["progress"]["conversation_id"])
    for e in save.get("trust_conversations", []):
        original = next(x for x in state["trust_events"] if x["id"] == e["event_id"])
        for key, day in [("response", original["day"]), ("result", original["resolved_day"])]:
            if e[key]["completed"]:
                conversation_days[day].append(e[key]["conversation_id"])
    for e in save.get("dispatch_conversations", []):
        original = next(x for x in state["dispatches"] if e["event_id"].startswith(x["id"] + ":"))
        if e["returning"]["completed"]:
            conversation_days[original["resolved_day"]].append(e["returning"]["conversation_id"])
    ui.append({"source": route, "historical_date": "2026-10-23", "command_counts": dict(kinds),
               "blocked_stops": stopped, "explicit_resumes": kinds["resume_autoplay"],
               "explicit_cancels": kinds["stop_autoplay"], "event_answers_per_day": dict(per_day),
               "completed_conversations_at_checkpoint": events, "template_counts": dict(templates),
               "repeated_template_visits": sum(n-1 for n in templates.values()),
               "recurrent_conversations_by_day": dict(conversation_days),
               "resume_actions": returns,
               "page_or_reveal_clicks": sum(" / 次へ" in a or " / 全文表示" in a for _,a in actions),
               "limitations": "Historical seed0 scripted UI evidence; blocked stops exclude paused-running dialogue episodes. Resume commands are observed, exact pause transitions are not in observer schema. Template repeat is not identical prose. Actions omit automatic resume calls; add one for final resume/start command. No reading-time measurement."})
(OUT / "player-ui-history.json").write_text(json.dumps(ui, ensure_ascii=False, indent=2), encoding="utf-8", newline="\n")
template = WORK / "cat_cafe_sim_tool/unity_preview_template/Runtime/Standalone"
unity = pathlib.Path("C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/CatCafePreview/Runtime/Standalone")
different = [p.name for p in template.glob("*.cs") if not (unity/p.name).exists() or p.read_text(encoding="utf-8-sig") != (unity/p.name).read_text(encoding="utf-8-sig")]
resource = WORK / "cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone"
manifest = {"normalized_core_source_differences": different,
            "sha256": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in [resource/"rules.json", resource/"stamina_growth.json"]},
            "trial_count": len(results), "closed_days_per_trial": 120,
            "resume_cases": sum(r["resume"] for r in results), "new_player_build": False,
            "historical_ui_source": "2026-10-23 IntegratedPlaytest; not rerun in this evaluation"}
(OUT / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8", newline="\n")
print("Simulation rows", len(results), "failures", sum(bool(r["failure"]) for r in results))
for r in results:
    print(r["id"], r["clear_day"], r["funds"], "fatigue", round(r["mean_closed_fatigue"],2), "stress",round(r["mean_closed_stress"],2),
          "max",max(c["max"] for c in r["cats"]), "post-share",round(r["post_clear"]["largest_share"],3))
for r in ui:
    print(r["source"], "blocked",len(r["blocked_stops"]),"resume",r["explicit_resumes"], "repeat",r["repeated_template_visits"],
          "max-day-answers",max(r["event_answers_per_day"].values()),"return-actions",[(x["day"],x["logged_ui_actions_after_answer"]+1) for x in r["resume_actions"]])
