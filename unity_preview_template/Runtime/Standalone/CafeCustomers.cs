using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public int ExtraWeekday(string customer)
        {
            var match = Regex.Match(customer, "^guest-([1-9][0-9]*)$");
            if (match.Success)
            {
                bool popular = int.TryParse(match.Groups[1].Value, out int number) && number > Rules.arrival_ticks.Length && number <= Rules.arrival_ticks.Length + Rules.popular_customer_count + Rules.second_popular_customer_count;
                int index = 0;
                foreach (char digit in match.Groups[1].Value) index = (index * 10 + digit - '0') % Rules.patterns.Length;
                var normal = Rules.patterns[(index + Rules.patterns.Length - 1) % Rules.patterns.Length].days;
                if (!popular) for (int day = 0; day < 7; day++) if (!normal.Contains(day)) return day;
            }
            return (int)((PythonRandom.Hash64(customer) >> 48) % 7);
        }
        public double DiscontentScore(string customer, int day)
            => EffectiveDiscontent(State.discontent.Find(r => r.customer_id == customer), day);
        private double EffectiveDiscontent(CafeDiscontent row, int day)
        {
            if (row == null) return 0;
            var last = row.suspensions.LastOrDefault();
            return last != null && row.score_day <= last.until && last.until < day ? Rules.discontent.return_score : row.score;
        }
        public bool CustomerAvailable(string customer, int day)
            => TrustAvailable(customer, day) && !State.discontent.Any(r => r.customer_id == customer && r.suspensions.Any(p => p.day < day && day <= p.until));
        public CafeArrival[] CustomerSchedule(int day)
        {
            int weekday = (Rules.start_weekday + day - 1) % 7;
            var schedule = new List<CafeArrival>();
            for (int i = 0; i < Rules.arrival_ticks.Length; i++)
                if (Rules.patterns[i % Rules.patterns.Length].days.Contains(weekday)) schedule.Add(new CafeArrival { customer_id = "guest-" + (i + 1), tick = Rules.arrival_ticks[i] });
            if(State.goal.Count>0 && ClearedGoalDay(1)>0 && day>ClearedGoalDay(1))schedule.Add(new CafeArrival {customer_id="advanced-white",tick=0});
            foreach(var purchase in State.expansion.Where(p=>p.seats>=4 && day>p.day))schedule.Add(new CafeArrival {customer_id=purchase.seats==4?"expanded-guest":"expanded-guest-"+(purchase.seats-3),tick=(purchase.seats-4)*6});
            foreach (var r in State.loyalty.Where(r => r.regular_day > 0 && day > r.regular_day && ExtraWeekday(r.customer_id) == weekday))
            {
                var existing = schedule.Find(a => a.customer_id == r.customer_id);
                if (existing != null) existing.tick = 0;
                else schedule.Add(new CafeArrival { customer_id = r.customer_id, tick = 0 });
            }
            for(int stage=1;stage<=2;stage++)
            {
                int cleared=ClearedGoalDay(stage), count=stage==1?Rules.popular_customer_count:Rules.second_popular_customer_count;
                if(cleared==0 || day<=cleared)continue;
                for(int i=0;i<count;i++)
                {
                    string id="guest-"+(Rules.arrival_ticks.Length+(stage==1?0:Rules.popular_customer_count)+i+1);
                    int tick=(2*i+1)*Rules.opening_ticks/(stage==1?2*(count+1):2*count);var existing=schedule.Find(r=>r.customer_id==id);
                    if(existing==null)schedule.Add(new CafeArrival {customer_id=id,tick=tick});else existing.tick=tick;
                }
            }
            if(State.goal.Count>0 && State.reservation.status=="accepted" && State.reservation.visit_day==day)schedule.Add(new CafeArrival {customer_id="reservation-longhair",tick=Rules.reservation.arrival_tick});
            if(State.goal.Count>0 && State.goal.Count==3 && ClearedGoalDay(3)>0 && day>ClearedGoalDay(3))MergeArrival(schedule,"vip-calico",0);
            if(State.goal.Count>0 && ClearedGoalDay(1)>0 && day>ClearedGoalDay(1))
            {
                if(Rules.play_customer.quiet_weekdays.Contains(weekday))MergeArrival(schedule,"advanced-quiet",0);
                if(Rules.play_customer.weekdays.Contains(weekday))MergeArrival(schedule,"advanced-play",0);
                if(Rules.contact_customer.weekdays.Contains(weekday))MergeArrival(schedule,"advanced-contact",0);
            }
            if(LonghairScheduled(day))MergeArrival(schedule,"advanced-longhair",0);
            var e = State.store_events.Find(r => r.day == day);
            if (e?.type == "rain")
            {
                var last = schedule.Where(a => Regex.IsMatch(a.customer_id, "^guest-[1-9][0-9]*$")).OrderByDescending(a => int.Parse(a.customer_id.Substring(6))).FirstOrDefault();
                if (last != null) schedule.Remove(last);
            }
            if (e?.type == "festival") schedule.Add(new CafeArrival { customer_id = "event-guest-" + day, tick = Rules.store_events.festival_customer_tick });
            return schedule.Where(a => CustomerAvailable(a.customer_id, day)).ToArray();
        }
        private static void MergeArrival(List<CafeArrival> schedule,string id,int tick)
        {var row=schedule.Find(a=>a.customer_id==id);if(row==null)schedule.Add(new CafeArrival {customer_id=id,tick=tick});else row.tick=tick;}
        internal void ChangeDiscontent(List<CafeDiscontent> rows, int day, string customer, double delta)
        {
            if (delta == 0) return;
            var row = rows.Find(r => r.customer_id == customer);
            double before = EffectiveDiscontent(row, day), after = Clamp(before + delta, Rules.discontent.threshold);
            if (after == before) return;
            if (row == null) { row = new CafeDiscontent { customer_id = customer }; rows.Add(row); }
            row.score = after; row.score_day = day;
            if (before < Rules.discontent.threshold && after >= Rules.discontent.threshold) row.suspensions.Add(new CafeSuspension { day = day, until = day + Rules.discontent.suspension_days });
        }
        internal void ApplyCustomerService(List<CafeLoyalty> loyalty, List<CafeDiscontent> discontent, int day, string customer, string satisfaction, string specialistResult = null)
        {
            if (satisfaction == "satisfied")
            {
                var row = loyalty.Find(r => r.customer_id == customer);
                if (row == null) { row = new CafeLoyalty { customer_id = customer }; loyalty.Add(row); }
                row.score = Math.Min(Rules.loyalty.threshold, row.score + Rules.loyalty.gain);
                if (row.regular_day == 0 && row.score >= Rules.loyalty.threshold) row.regular_day = day;
            }
            ChangeDiscontent(discontent, day, customer, (customer=="advanced-white" && specialistResult=="failure"?Rules.discontent.advanced_failure_gain:0) + (satisfaction == "satisfied" ? -Rules.discontent.good_service_recovery : satisfaction == "dissatisfied" ? Rules.satisfaction.dissatisfied_discontent_gain : 0));
        }
        private void ValidateCustomers(CafeSnapshot s)
        {
            if (s.loyalty == null || s.discontent == null || s.loyalty.Select(r => r.customer_id).Distinct().Count() != s.loyalty.Count || s.discontent.Select(r => r.customer_id).Distinct().Count() != s.discontent.Count) throw new ArgumentException("Invalid customer IDs");
            foreach (var r in s.loyalty)
                if (string.IsNullOrEmpty(r.customer_id) || !Range(r.score, 0, Rules.loyalty.threshold) || r.regular_day < 0 || r.regular_day > s.day || (r.regular_day == 0) != (r.score < Rules.loyalty.threshold)) throw new ArgumentException("Invalid loyalty state");
            foreach (var r in s.discontent)
            {
                if (string.IsNullOrEmpty(r.customer_id) || !Range(r.score, 0, Rules.discontent.threshold) || r.score_day < 1 || r.score_day > s.day || r.suspensions == null) throw new ArgumentException("Invalid discontent state");
                int previous = 0;
                foreach (var p in r.suspensions)
                {
                    if (p.day <= previous || p.day > s.day || p.until != p.day + Rules.discontent.suspension_days) throw new ArgumentException("Invalid customer suspension");
                    previous = p.until;
                }
            }
            var expectedLoyalty = new List<CafeLoyalty>(); var expectedDiscontent = new List<CafeDiscontent>();
            foreach (var h in s.history)
            {
                if (h.customer_records == null || h.customer_records.Count != h.arrivals || h.customer_records.Select(r => r.customer_id).Distinct().Count() != h.customer_records.Count || h.customer_records.Count(r => r.satisfaction != null) != h.completed) throw new ArgumentException("Invalid customer history");
                foreach (var r in h.customer_records)
                {
                    if (string.IsNullOrEmpty(r.customer_id) || !new[] { "queue_full", "wait_timeout", "closing", "interaction_exhausted", "interaction_time_limit" }.Contains(r.departure) || r.satisfaction != null && !new[] { "satisfied", "normal", "dissatisfied" }.Contains(r.satisfaction)) throw new ArgumentException("Invalid customer outcome");
                    ReplayCustomer(expectedLoyalty, expectedDiscontent, h.day, r.customer_id, r.departure, r.satisfaction, r.specialist_result);
                    ReplayTrustRecovery(s, expectedDiscontent, h.day, r.customer_id, r.satisfaction);
                }
            }
            foreach (var v in s.visits) { ReplayCustomer(expectedLoyalty, expectedDiscontent, s.day, v.id, v.departure, v.satisfaction?.level, v.specialist_result); ReplayTrustRecovery(s, expectedDiscontent, s.day, v.id, v.satisfaction?.level); }
            if (expectedLoyalty.Count != s.loyalty.Count || expectedDiscontent.Count != s.discontent.Count) throw new ArgumentException("Customer totals disagree with history");
            foreach (var r in expectedLoyalty) if (!s.loyalty.Any(a => a.customer_id == r.customer_id && a.score == r.score && a.regular_day == r.regular_day)) throw new ArgumentException("Loyalty disagrees with history");
            foreach (var r in expectedDiscontent)
            {
                var actual = s.discontent.Find(a => a.customer_id == r.customer_id);
                if (actual == null || actual.score != r.score || actual.score_day != r.score_day || actual.suspensions.Count != r.suspensions.Count || actual.suspensions.Where((p, i) => p.day != r.suspensions[i].day || p.until != r.suspensions[i].until).Any()) throw new ArgumentException("Discontent disagrees with history");
            }
        }
        private void ReplayCustomer(List<CafeLoyalty> loyalty, List<CafeDiscontent> discontent, int day, string id, string departure, string satisfaction, string specialistResult)
        {
            if (departure == "queue_full" || departure == "wait_timeout") ChangeDiscontent(discontent, day, id, Rules.discontent.waiting_gain);
            if (satisfaction != null) ApplyCustomerService(loyalty, discontent, day, id, satisfaction, specialistResult);
        }
    }
}
