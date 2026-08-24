using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    [RequireComponent(typeof(CombatActor))]
    public sealed class AutoCombatController : MonoBehaviour
    {
        private CombatActor Actor;
        private BattleContext Context;
        private MvpAutoCombatSettings Settings = new MvpAutoCombatSettings();
        private float AttackTimer;

        public void Initialize(BattleContext _Context)
        {
            Initialize(_Context, Settings);
        }

        public void Initialize(BattleContext _Context, MvpAutoCombatSettings _Settings)
        {
            Context = _Context;
            Settings = _Settings ?? new MvpAutoCombatSettings();
            Actor = GetComponent<CombatActor>();
            AttackTimer = Random.Range(Settings.InitialAttackDelayMin, Settings.ClampInitialDelayMax());
        }

        private void Awake()
        {
            Actor = GetComponent<CombatActor>();
        }

        private void Update()
        {
            if (Context == null || Actor == null || !Actor.IsAlive)
                return;

            AttackTimer -= Time.deltaTime;

            CombatActor target = Context.FindNearestEnemy(Actor);
            Actor.SetTarget(target);

            if (target == null)
            {
                Actor.Model.SetState(ActorState.Search);
                return;
            }

            float distance = Vector2.Distance(transform.position, target.transform.position);
            if (distance > Actor.Model.Stats.AttackRange)
            {
                MoveToward(target);
                return;
            }

            Actor.Model.SetState(ActorState.Attack);
            Actor.Face(target.transform.position);

            if (AttackTimer <= 0f)
            {
                AttackTimer = Actor.Model.Stats.AttackInterval;
                target.TakeBasicAttack(Actor, Random.value);
            }
        }

        private void MoveToward(CombatActor _Target)
        {
            Actor.Model.SetState(ActorState.Move);
            Actor.Face(_Target.transform.position);
            transform.position = Vector3.MoveTowards(
                transform.position,
                _Target.transform.position,
                Actor.Model.Stats.MoveSpeed * Time.deltaTime);
        }
    }
}
