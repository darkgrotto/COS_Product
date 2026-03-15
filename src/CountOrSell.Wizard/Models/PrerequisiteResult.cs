namespace CountOrSell.Wizard.Models;

public record PrerequisiteResult(bool AllMet, IReadOnlyList<MissingPrerequisite> Missing);
public record MissingPrerequisite(string Name, string InstallInstructions);
