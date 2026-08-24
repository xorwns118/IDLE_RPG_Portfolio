namespace IdleRPG.Domain.Combat
{
    public readonly struct DamageResult
    {
        public static DamageResult None => new DamageResult(0f, false);

        public DamageResult(float _FinalDamage, bool _IsCritical)
        {
            FinalDamage = _FinalDamage;
            IsCritical = _IsCritical;
        }

        public float FinalDamage { get; }
        public bool IsCritical { get; }
    }
}
