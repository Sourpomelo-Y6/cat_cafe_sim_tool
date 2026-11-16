using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeSatoDispatch
    {
        public string cat_id;
        public int return_access, requested_day, accepted_day, declined_day, returned_day;
        public bool refused_return, returned_after_service, epilogue_read;
        public CafeConversationProgress conversation;
    }

    [Serializable] public sealed class CafeNamedDispatchArchive
    {
        public string customer_id;
        public CafeSatoDispatch request;
    }

    [Serializable] public sealed class CafeSatoAffinityScene
    {
        public string cat_id;
        public CafeConversationProgress conversation;
    }

    [Serializable] public sealed class CafeNamedDispatchState
    {
        public string customer_id;
        public CafeSatoDispatch request;
        public List<CafeSatoAffinityScene> affinity = new List<CafeSatoAffinityScene>();
    }

    public sealed partial class CafeSimulation
    {
        public const string Sato = "guest-1";
        public const string Suzuki = "guest-2";
        public const string SuzukiDestination = "suzuki_request";
        public static readonly IReadOnlyList<string> NamedDispatchCustomers = Array.AsReadOnly(new[]{Sato,Suzuki});
        public static string NamedCustomerName(string customer)
            => customer==Sato?"佐藤さん":customer==Suzuki?"鈴木さん":customer;

        private static string NamedPrefix(string customer)
            => customer==Sato?"sato":customer==Suzuki?"suzuki":"named-"+customer;

        private static string CustomerFromNamedKey(string key,string suffix)
        {
            if(string.IsNullOrEmpty(key) || !key.EndsWith(suffix,StringComparison.Ordinal))return null;
            string prefix=key.Substring(0,key.Length-suffix.Length);
            if(prefix=="sato")return Sato;
            if(prefix=="suzuki")return Suzuki;
            if(!prefix.StartsWith("named-",StringComparison.Ordinal))return null;
            string customer=prefix.Substring(6);
            return !NamedDispatchCustomers.Contains(customer) && CafeNamedDispatchSettings.CustomerId(customer)?customer:null;
        }

        public static string NamedDestinationCustomer(string destination) => CustomerFromNamedKey(destination,"_request");
        public static string NamedMissingCustomer(string reason)
            => CustomerFromNamedKey(reason,"_expired") ?? CustomerFromNamedKey(reason,"_refused");
        public static string NamedEpilogueId(string customer) => NamedPrefix(customer)+".epilogue.v1";
        private static string NamedAdoptionSource(string customer) => NamedPrefix(customer)+"-return";
        public static bool IsNamedAdoption(string source) => CustomerFromNamedKey(source,"-return")!=null;
        internal static readonly DispatchRules SuzukiVisit = new DispatchRules
            { id=SuzukiDestination, name="鈴木さんへの指名派遣", days=1, max_fatigue=60 };

        private readonly string[] enabledNamedCustomers;
        internal string[] ReplayNamedCustomers;
        internal IEnumerable<string> EnabledNamedCustomers => ReplayNamedCustomers ?? enabledNamedCustomers;
        internal static string[] NamedCustomersForRules(CafeRules rules)
        {
            var settings=rules.named_dispatch??Array.Empty<CafeNamedDispatchSettings>();
            return NamedDispatchCustomers.Where(c=>settings.All(r=>r.customer_id!=c || r.enabled))
                .Concat(settings.Where(r=>r.enabled && !NamedDispatchCustomers.Contains(r.customer_id))
                    .Select(r=>r.customer_id).OrderBy(c=>c,StringComparer.Ordinal)).ToArray();
        }
        private static IEnumerable<string> RecordedNamedCustomers(CafeSnapshot state)
            => NamedDispatchCustomers.Concat(state.named_dispatch.Select(row=>row.customer_id));
        public IEnumerable<string> NamedCustomers => RecordedNamedCustomers(State).Concat(EnabledNamedCustomers).Distinct();

        private CafeNamedDispatchSettings NamedSettings(string customer)
            => Rules.named_dispatch?.FirstOrDefault(row=>row.customer_id==customer);

        public CafeSatoDispatch NamedRequest(string customer) => NamedRequest(State,customer);
        private static CafeSatoDispatch NamedRequest(CafeSnapshot state,string customer)
            => customer==Sato?state.sato_dispatch:customer==Suzuki?state.suzuki_dispatch:
                state.named_dispatch?.Find(row=>row.customer_id==customer)?.request;

        private static IEnumerable<CafeSatoDispatch> NamedRequests(CafeSnapshot state,string customer)
            => state.named_dispatch_archive.Where(row=>row.customer_id==customer).Select(row=>row.request)
                .Append(NamedRequest(state,customer)).Where(request=>request!=null);

        internal CafeSatoDispatch NamedTripRequest(CafeDispatch trip)
            => NamedTripRequest(State,trip);

        private static CafeSatoDispatch NamedTripRequest(CafeSnapshot state,CafeDispatch trip)
            => NamedDestinationCustomer(trip.destination) is string customer
                ? NamedRequests(state,customer).SingleOrDefault(request=>request.accepted_day==trip.started_day)
                : null;

        public List<CafeSatoAffinityScene> NamedAffinity(string customer)
            => NamedAffinity(State,customer) ?? new List<CafeSatoAffinityScene>();
        private static List<CafeSatoAffinityScene> NamedAffinity(CafeSnapshot state,string customer)
            => customer==Sato?state.sato_affinity:customer==Suzuki?state.suzuki_affinity:
                state.named_dispatch.Find(row=>row.customer_id==customer)?.affinity;

        private List<CafeSatoAffinityScene> EnsureNamedAffinity(string customer)
        {
            var affinity=NamedAffinity(State,customer);
            if(affinity!=null)return affinity;
            var row=new CafeNamedDispatchState {customer_id=customer};
            State.named_dispatch.Add(row);
            return row.affinity;
        }
        public const string SatoDestination = "sato_request";
        private const double SatoAffinity = 80;
        private const double SatoDiscontent = 50;
        internal static readonly DispatchRules SatoVisit = new DispatchRules
            { id=SatoDestination, name="佐藤さんへの指名派遣", days=1, max_fatigue=60 };

        private static CafeConversationProgress SatoScene(string id, string branch)
            => new CafeConversationProgress { conversation_id=id, branch_id=branch, viewed_branches=Array.Empty<string>() };

        private void ObserveSatoAffinity(string catId, string customer, double affinity)
        {
            if (State.import_history != null || !EnabledNamedCustomers.Contains(customer) || affinity < SatoAffinity || NamedAffinity(customer).Any(e => e.cat_id == catId)) return;
            EnsureNamedAffinity(customer).Add(new CafeSatoAffinityScene { cat_id=catId, conversation=SatoScene(NamedPrefix(customer)+".affinity.v1","affinity") });
        }

        private void PresentSatoDispatch()
        {
            foreach(var customer in NamedCustomers)PresentNamedDispatch(customer);
        }

        private void PresentNamedDispatch(string customer)
        {
            if (State.import_history != null) return;
            var request = NamedRequest(customer);
            bool reissue = request?.return_access == AllowReturnAccess
                && State.day == request.returned_day + NamedDispatchRetryDelay;
            if (request != null && !reissue)
            {
                if (request.returned_day != 0 || request.declined_day != 0) return;
                if (State.day == request.requested_day + 1 && request.accepted_day == 0 && request.declined_day == 0)
                    request.conversation = SatoScene(NamedPrefix(customer)+".request.v1","introduction");
                if (request.accepted_day != 0 || request.declined_day != 0 || State.day < request.requested_day + 2
                    || NamedMissing(State,customer,request) != null) return;
                var cat = State.cats.Single(c => c.id == request.cat_id);
                if (cat.activity == "adopted") request.declined_day = State.day;
                else if (cat.activity == "cafe") StartSatoMissing(cat, NamedPrefix(customer)+"_expired");
                return;
            }

            if (!EnabledNamedCustomers.Contains(customer) || DiscontentScore(customer, State.day) < SatoDiscontent
                || State.trust_customers.Any(c => c.customer_id == customer && c.status != "stable")
                || State.adoption?.events.Any(e => e.customer_id == customer && e.status == "waiting") == true) return;
            var candidate = State.relationships
                .Where(r => r.customer_id == customer && r.affinity >= SatoAffinity && NamedAffinity(customer).Any(e => e.cat_id == r.cat_id))
                .Where(r => !NamedCustomers.Any(other => NamedRequest(other) is CafeSatoDispatch active
                    && active.cat_id==r.cat_id && active.returned_day==0 && active.declined_day==0))
                .Where(r => State.adoption?.events.Any(e=>e.cat_id==r.cat_id && e.status=="waiting") != true)
                .Where(r => State.cats.Any(c => c.id == r.cat_id && c.activity == "cafe" && c.health == "healthy"
                    && !c.cannot_continue && c.stamina > 0 && c.fatigue <= SatoVisit.max_fatigue))
                .OrderByDescending(r => r.affinity).ThenBy(r => r.cat_id, StringComparer.Ordinal).FirstOrDefault();
            if (candidate == null) return;
            if (State.cats.Count(c => c.activity == "cafe" && c.health == "healthy" && !c.cannot_continue && c.stamina > 0) <= State.seat_count) return;
            if (reissue)
                State.named_dispatch_archive.Add(new CafeNamedDispatchArchive {customer_id=customer,request=request});
            request = new CafeSatoDispatch { cat_id=candidate.cat_id, requested_day=State.day,
                conversation=SatoScene(NamedPrefix(customer)+".request.v1","introduction") };
            if(customer==Sato)State.sato_dispatch=request;
            else if(customer==Suzuki)State.suzuki_dispatch=request;
            else State.named_dispatch.Single(row=>row.customer_id==customer).request=request;
        }

        // Zero identifies returns resolved before this choice was introduced.
        public const int AwaitingReturnAccess=1, AllowReturnAccess=2, BanReturnAccess=3;
        internal bool LegacyNamedReturnReplay;
        private const int NamedDispatchRetryDelay=8;

        public bool NamedReturnAccessWaiting => NamedCustomers.Any(NamedReturnAccessPending);

        public bool NamedReturnAccessPending(string customer)
            => NamedRequest(customer)?.return_access==AwaitingReturnAccess;

        private void ResolveNamedReturnAccess(string customer,string choice)
        {
            var request=NamedRequest(customer);
            if(choice!="allow" && choice!="ban")throw new ArgumentException("今後の来店を選んでください。");
            int access=choice=="allow"?AllowReturnAccess:BanReturnAccess;
            if(request?.return_access==access)return;
            if(request?.return_access!=AwaitingReturnAccess || request.returned_day!=State.day)
                throw new InvalidOperationException("回答待ちの帰還案件を選んでください。");
            request.return_access=access;
            State.revision++;
        }

        private bool NamedCustomerBanned(string customer,int day)
            => NamedRequest(customer) is CafeSatoDispatch e && e.return_access==BanReturnAccess && day>=e.returned_day;

        public const string SatoAdoptionSource = "sato-return";

        private void PresentSatoAdoption()
        {
            foreach(var customer in NamedCustomers)PresentNamedAdoption(customer);
        }

        private void PresentNamedAdoption(string customer)
        {
            var missing = NamedMissing(State,customer,NamedRequest(customer));
            if (missing == null || missing.resolved_day == 0 || missing.resolved_day + 1 != State.day
                || State.adoption?.enabled != true || NamedRequest(customer).return_access>=BanReturnAccess
                || NamedReturnAccessPending(customer)) return;
            var cat = State.cats.Single(c => c.id == missing.cat_id);
            if (cat.activity != "cafe") return;
            double affinity = State.relationships.Find(r => r.cat_id == cat.id && r.customer_id == customer)?.affinity ?? 0;
            double player = PlayerCat(cat.id).affinity;
            if (affinity < Math.Max(80,Rules.adoption_threshold) || affinity <= player || player > 40) return;
            State.adoption.events.Add(new CafeAdoptionEvent { id="adoption-"+State.day+"-"+cat.id,
                cat_id=cat.id, customer_id=customer, source=NamedAdoptionSource(customer), day=State.day, tick=0,
                guest_affinity=affinity, player_affinity=player, threshold=Rules.adoption_threshold });
        }

        public bool SatoEpilogueAvailable(string catId) => NamedEpilogueCustomer(catId)!=null;
        public string NamedEpilogueCustomer(string catId)
            => NamedCustomers.FirstOrDefault(c=>NamedEpilogueAvailable(State,catId,c));

        private static bool NamedEpilogueAvailable(CafeSnapshot s,string catId,string customer)
            => s.adoption?.events.Any(e => e.source == NamedAdoptionSource(customer) && e.customer_id==customer && e.cat_id == catId
                && e.status == "resolved" && e.choice == "accept" && s.day - e.resolved_day >= 3) == true;

        internal CafeSatoDispatch NamedEpilogueRequest(string catId)
        {
            var customer = NamedEpilogueCustomer(catId);
            if (customer == null) return null;
            var adoption = State.adoption.events.Single(e=>e.cat_id==catId && e.choice=="accept");
            return NamedRequests(State,customer).Single(e=>e.cat_id==catId && e.returned_day==adoption.day-1);
        }

        private static CafeMissingCase NamedMissing(CafeSnapshot state,string customer,CafeSatoDispatch request)
        {
            if (request == null) return null;
            int next = NamedRequests(state,customer).Where(e=>e.requested_day>request.requested_day)
                .Select(e=>e.requested_day).DefaultIfEmpty(int.MaxValue).Min();
            return state.missing_cases.SingleOrDefault(e=>NamedMissingCustomer(e.reason)==customer
                && e.departed_day>=request.requested_day && e.departed_day<next);
        }

        private static int SatoTrustDay(CafeSnapshot state, string customer, int day)
        {
            var e = NamedRequest(state,customer);
            if (e == null || day < e.requested_day || day == int.MaxValue) return day;
            int end = Math.Max(e.returned_day, e.declined_day);
            return end == 0 ? int.MaxValue : Math.Max(day, end + 1);
        }

        public bool NamedDispatchAwaitingAnswer(string customer) => NamedRequest(customer) is CafeSatoDispatch e && e.accepted_day == 0
            && e.declined_day == 0 && State.day < e.requested_day + 2;

        public string SatoDispatchResponseReason(bool accept,string customer=Sato)
        {
            if (!NamedDispatchAwaitingAnswer(customer)) return "この依頼の回答期限は終了しました。";
            if (State.game_over != null || State.phase != "preparation") return "営業準備中に回答してください。";
            return accept ? DispatchReason(NamedRequest(customer).cat_id, NamedPrefix(customer)+"_request") : "";
        }

        private void RespondSatoDispatch(string choice,string customer)
        {
            if (choice != "accept" && choice != "decline") throw new ArgumentException("指名依頼への回答を選んでください。");
            bool accept = choice == "accept";
            string reason = SatoDispatchResponseReason(accept,customer);
            if (reason != "") throw new InvalidOperationException(reason);
            var request = NamedRequest(customer);
            if (!accept) { request.declined_day=State.day; return; }
            Dispatch(request.cat_id, NamedPrefix(customer)+"_request");
            request.accepted_day=State.day;
            request.refused_return=DiscontentScore(customer, State.day) >= Rules.discontent.threshold;
        }

        private void StartSatoMissing(CafeCat cat, string reason)
        {
            cat.activity="missing";
            cat.working=false;
            cat.kitten=false;
            cat.missing_remaining=Rules.missing_days;
            State.missing_cases.Add(new CafeMissingCase { id="missing-"+State.day+"-"+cat.id, cat_id=cat.id,
                departed_day=State.day, reason=reason });
        }

        private bool SatoRefusal(CafeSnapshot state, CafeDispatch e)
            => NamedTripRequest(state,e)?.refused_return == true;

        private void FinishSatoDispatch(CafeDispatch e, CafeCat cat)
        {
            string customer=NamedDestinationCustomer(e.destination);
            if (customer==null) return;
            var request=NamedRequest(customer);
            if (SatoRefusal(State,e)) StartSatoMissing(cat,NamedPrefix(customer)+"_refused");
            else
            {
                request.returned_day=State.day;
                request.returned_after_service=State.phase=="closed";
                ChangeDiscontent(State.discontent,State.day,customer,-Rules.discontent.good_service_recovery);
            }
        }

        private void ReplaySatoReturn(CafeSnapshot s, List<CafeDiscontent> scores, int day, bool afterService)
        {
            foreach(var customer in RecordedNamedCustomers(s))
            foreach(var e in NamedRequests(s,customer))
                if (e.accepted_day > 0 && !e.refused_return && e.returned_day == day && e.returned_after_service == afterService)
                    ChangeDiscontent(scores, day, customer, -Rules.discontent.good_service_recovery);
        }

        private void ValidateSatoDispatch(CafeSnapshot s)
        {
            CafeMissingDialogue.Validate(s);
            var customers=RecordedNamedCustomers(s).ToArray();
            if(s.named_dispatch.Count>500 || s.named_dispatch.Any(row=>!CafeNamedDispatchSettings.CustomerId(row.customer_id))
                || customers.Distinct().Count()!=customers.Length
                || s.dispatches.Any(e=>NamedDestinationCustomer(e.destination) is string id && !customers.Contains(id))
                || s.missing_cases.Any(e=>NamedMissingCustomer(e.reason) is string id && !customers.Contains(id)))
                throw new ArgumentException("Invalid named dispatch customers");
            if(s.named_dispatch_archive.Any(row=>row?.request==null || !customers.Contains(row.customer_id)))
                throw new ArgumentException("Invalid named dispatch history");
            foreach(var customer in customers)ValidateNamedDispatch(s,customer);
            var active=customers.Select(c=>NamedRequest(s,c)).Where(e=>e!=null && e.returned_day==0 && e.declined_day==0);
            if(active.GroupBy(e=>e.cat_id).Any(g=>g.Count()>1))throw new ArgumentException("Overlapping named requests");
        }

        private void ValidateNamedDispatch(CafeSnapshot s,string customer)
        {
            if (NamedAffinity(s,customer) == null || NamedAffinity(s,customer).Any(e => e == null || !s.cats.Any(c => c.id == e.cat_id)
                || !ValidSatoScene(e.conversation,NamedPrefix(customer)+".affinity.v1","affinity",2))
                || NamedAffinity(s,customer).Select(e => e.cat_id).Distinct().Count() != NamedAffinity(s,customer).Count)
                throw new ArgumentException("Invalid Sato affinity scenes");

            var requests = NamedRequests(s,customer).ToArray();
            var trips = s.dispatches.Where(e=>NamedDestinationCustomer(e.destination)==customer).ToArray();
            if ((requests.Length>0 && NamedRequest(s,customer)==null)
                || trips.Length!=requests.Count(e=>e.accepted_day>0))
                throw new ArgumentException("Missing named dispatch request");
            int missingCount = 0;
            for (int i=0; i<requests.Length; i++)
            {
                var request = requests[i];
                if (i>0 && (requests[i-1].return_access!=AllowReturnAccess
                    || request.requested_day!=requests[i-1].returned_day+NamedDispatchRetryDelay))
                    throw new ArgumentException("Invalid named dispatch reissue");
                var trip = trips.SingleOrDefault(e=>e.started_day==request.accepted_day);
                var missing = NamedMissing(s,customer,request);
                if (missing!=null) missingCount++;
                ValidateNamedRequest(s,customer,request,trip,missing);
            }
            if (missingCount!=s.missing_cases.Count(e=>NamedMissingCustomer(e.reason)==customer))
                throw new ArgumentException("Missing named dispatch for a return record");
        }

        private void ValidateNamedRequest(CafeSnapshot s,string customer,CafeSatoDispatch request,CafeDispatch trip,CafeMissingCase missing)
        {
            if (request.epilogue_read && !NamedEpilogueAvailable(s,request.cat_id,customer))
                throw new ArgumentException("Sato epilogue read before unlock");
            if(request.return_access<0 || request.return_access>BanReturnAccess
                || request.return_access!=0 && (missing==null || missing.resolved_day==0)
                || request.return_access==AwaitingReturnAccess && request.returned_day!=s.day)
                throw new ArgumentException("Invalid access decision for named return");
            if (!NamedAffinity(s,customer).Any(e => e.cat_id == request.cat_id) || request.requested_day < 2 || request.requested_day > s.day
                || request.accepted_day < 0 || request.accepted_day > s.day || request.declined_day < 0 || request.declined_day > s.day
                || request.returned_day < 0 || request.returned_day > s.day
                || !ValidSatoScene(request.conversation,NamedPrefix(customer)+".request.v1","introduction",2)
                || (request.accepted_day == 0) != (trip == null) || (request.refused_return && trip == null)
                || (trip != null && (trip.cat_id != request.cat_id || request.declined_day != 0
                    || trip.started_day != request.accepted_day || trip.started_day < request.requested_day || trip.started_day >= request.requested_day + 2))
                || (request.declined_day != 0 && (missing != null || request.declined_day < request.requested_day))
                || (missing != null && (missing.cat_id != request.cat_id || missing.resolved_day != request.returned_day
                    || (request.refused_return ? missing.reason != NamedPrefix(customer)+"_refused" || trip.resolved_day != missing.departed_day
                        : trip != null || missing.reason != NamedPrefix(customer)+"_expired" || missing.departed_day < request.requested_day + 2)))
                || (missing != null && missing.resolved_day == 0 && !s.cats.Any(c => c.id == missing.cat_id
                    && c.activity == "missing" && !c.working && !c.kitten))
                || (trip?.status == "resolved" && request.refused_return && missing == null)
                || (missing == null && request.returned_day != (trip?.resolved_day ?? 0)))
                throw new ArgumentException("Invalid Sato dispatch request");
        }

        private static bool ValidSatoScene(CafeConversationProgress p, string id, string branch, int length)
            => p != null && p.conversation_id == id && p.branch_id == branch && p.node >= 0 && p.node <= length
                && p.page_offset >= 0 && p.page_offset <= 10000 && p.viewed_branches != null
                && (p.completed ? p.node == length && p.viewed_branches.SequenceEqual(new[]{branch}) : p.viewed_branches.Length == 0);
    }
}
