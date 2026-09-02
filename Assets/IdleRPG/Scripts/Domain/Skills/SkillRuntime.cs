using System;

namespace IdleRPG.Domain.Skills
{
    public sealed class SkillRuntime
    {
        public SkillRuntime(SkillDefinition _Definition)
        {
            Definition = _Definition ?? throw new ArgumentNullException(nameof(_Definition));
        }

        public SkillDefinition Definition { get; }
        public float RemainingCooldownSeconds { get; private set; }
        public bool IsReady => RemainingCooldownSeconds <= 0f;

        public void Tick(float _DeltaSeconds)
        {
            if (RemainingCooldownSeconds <= 0f)
                return;

            RemainingCooldownSeconds = Math.Max(0f, RemainingCooldownSeconds - Math.Max(0f, _DeltaSeconds));
        }

        public void StartCooldown()
        {
            RemainingCooldownSeconds = Definition.CooldownSeconds;
        }

        public void ResetCooldown()
        {
            RemainingCooldownSeconds = 0f;
        }
    }
}
