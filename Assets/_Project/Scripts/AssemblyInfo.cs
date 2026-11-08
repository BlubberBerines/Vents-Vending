using System.Runtime.CompilerServices;

// Internal configuration APIs (Configure, SetWorldPrefab, …) are for editor tooling and tests only.
[assembly: InternalsVisibleTo("VentsAndVending.Editor")]
[assembly: InternalsVisibleTo("VentsAndVending.Tests.EditMode")]
[assembly: InternalsVisibleTo("VentsAndVending.Tests.PlayMode")]
