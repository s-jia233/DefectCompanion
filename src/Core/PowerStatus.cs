namespace DefectCompanion
{
    public sealed class PowerStatus
    {
        public readonly string Key, Name, Description;
        public readonly int Amount;
        public PowerStatus(string key, string name, int amount, string description)
        { Key = key; Name = name; Amount = amount; Description = description; }
        public string Tooltip { get { return Name + " · " + Amount + "\n" + Description; } }
    }
}
