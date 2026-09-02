using System;
using System.Collections.Generic;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Combat;

namespace IdleRPG.Domain.Skills
{
    public sealed class SkillLoadout
    {
        public const int MaxSlots = 4;

        private readonly SkillRuntime[] SlotsValue = new SkillRuntime[MaxSlots];

        public SkillLoadout()
        {
        }

        public SkillLoadout(IEnumerable<SkillDefinition> _Definitions)
        {
            if (_Definitions == null)
                return;

            int slotIndex = 0;
            foreach (SkillDefinition definition in _Definitions)
            {
                if (slotIndex >= MaxSlots)
                    break;

                SetSlot(slotIndex, definition);
                slotIndex++;
            }
        }

        public IReadOnlyList<SkillRuntime> Slots => SlotsValue;

        public int FilledSlotCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < SlotsValue.Length; i++)
                {
                    if (SlotsValue[i] != null)
                        count++;
                }

                return count;
            }
        }

        public bool HasAnySkill => FilledSlotCount > 0;

        public SkillRuntime GetSlot(int _Index)
        {
            return SlotsValue[RequireSlotIndex(_Index)];
        }

        public void SetSlot(int _Index, SkillDefinition _Definition)
        {
            SlotsValue[RequireSlotIndex(_Index)] = _Definition != null ? new SkillRuntime(_Definition) : null;
        }

        public void TickCooldowns(float _DeltaSeconds)
        {
            for (int i = 0; i < SlotsValue.Length; i++)
            {
                if (SlotsValue[i] != null)
                    SlotsValue[i].Tick(_DeltaSeconds);
            }
        }

        public void ResetCooldowns()
        {
            for (int i = 0; i < SlotsValue.Length; i++)
            {
                if (SlotsValue[i] != null)
                    SlotsValue[i].ResetCooldown();
            }
        }

        public SkillRuntime SelectBestReadySkill(
            ActorModel _Caster,
            ActorModel _Target,
            float _Distance,
            ISkillExecutor _Executor)
        {
            if (_Executor == null)
                return null;

            SkillRuntime bestSkill = null;
            int bestPriority = int.MinValue;
            for (int i = 0; i < SlotsValue.Length; i++)
            {
                SkillRuntime skill = SlotsValue[i];
                if (skill == null || !_Executor.CanExecute(skill, _Caster, _Target, _Distance))
                    continue;

                if (bestSkill == null || skill.Definition.Priority > bestPriority)
                {
                    bestSkill = skill;
                    bestPriority = skill.Definition.Priority;
                }
            }

            return bestSkill;
        }

        private static int RequireSlotIndex(int _Index)
        {
            if (_Index < 0 || _Index >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(_Index), "Skill slot index must be between 0 and 3.");

            return _Index;
        }
    }
}
