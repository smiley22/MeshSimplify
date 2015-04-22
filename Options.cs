using Plossum.CommandLine;

namespace MeshSimplify {
	/// <summary>
	/// Repräsentiert die möglichen Kommandozeilenparameter, die an das Programm übergeben
	/// werden können.
	/// </summary>
	[CommandLineManager(EnabledOptionStyles = OptionStyles.Group | OptionStyles.LongUnix)]
	public class Options {
		/// <summary>
		/// true, um ausführliche Programmausgaben zu erzeugen; andernfalls false.
		/// </summary>
		[CommandLineOption(Name = "v", Aliases = "verbose",
			Description = "Produces verbose output.")]
		public bool Verbose {
			get;
			set;
		}

		/// <summary>
		/// Der Name des Algorithmus, der zur Vereinfachung der Eingabemesh benutzt werden soll.
		/// </summary>
		[CommandLineOption(Name = "a", Aliases = "algorithm", MaxOccurs = 1,
		  Description = "Specifies the simplification algorithm to use. Currently the only valid " +
		  "value for this option is `PairContract'. More algorithms may be added in the future.")]
		public string Algorithm {
			get;
			set;
		}

		/// <summary>
		/// Die Anzahl der Facetten, die die vereinfachte Mesh anstreben soll.
		/// </summary>
		[CommandLineOption(Name = "n", Aliases = "num-faces",  MinOccurs = 1, MinValue = 1,
			Description = "Specifies the number of faces to reduce the input mesh to.")]
		public int TargetFaceCount {
			get;
			set;
		}

		/// <summary>
		/// Der maximale Abstand, den zwei Vertices haben dürfen, um als Vertexpaar aufgefasst
		/// zu werden. Diese Option ist nur für den Algorithmus `PairContract' relevant.
		/// </summary>
		[CommandLineOption(Name = "d", Aliases = "distance-threshold",
			Description = "Specifies the distance-threshold value for pair-contraction. This " +
			"option is only applicable for the `PairContract' algorithm and defaults to 0.")]
		public float DistanceThreshold {
			get;
			set;
		}

		/// <summary>
		/// Der Name der Ausgabedatei.
		/// </summary>
		/// <remarks>
		/// Wenn diese Option nicht angegeben wird, wird die Ausgabedatei nach der Eingabedatei mit
		/// dem zusätzlichen Suffix 'out' benannt.
		/// </remarks>
		[CommandLineOption(Name = "o",
			Description = "Specifies the output file.")]
		public string OutputFile {
			get;
			set;
		}

		/// <summary>
		/// true, um die Programmausführung bei nicht-wohlgeformter Eingabedatei abzubrechen.
		/// </summary>
		[CommandLineOption(Name = "s", Aliases = "strict",
			Description = "Specifies strict mode. This will cause simplification to fail " +
			"if the input mesh is malformed or any other anomaly occurs.")]
		public bool Strict {
			get;
			set;
		}

		/// <summary>
		/// true, um eine Progressive Mesh zu erzeugen.
		/// </summary>
		[CommandLineOption(Name = "p", Aliases = "progressive-mesh",
			Description = "Adds vertex-split records to the resulting .obj file, effectively " +
			"creating a progressive-mesh representation of the input mesh.")]
		public bool ProgressiveMesh {
			get;
			set;
		}
		
		/// <summary>
		/// true, um aus der Eingabe (Progressive) Mesh eine detaillierte Version zu
		/// erzeugen.
		/// </summary>
		[CommandLineOption(Name = "r", Aliases = "restore-mesh",
			Description="Expands the input progressive-mesh to the desired face-count. " +
			"If this option is specified, num-faces refers to the number of faces to " +
			"restore the mesh to.")]
		public bool Restore {
			get;
			set;
		}

		/// <summary>
		/// true, um Versionsinformationen anzuzeigen; andernfalls false.
		/// </summary>
		[CommandLineOption(Name = "version", Description = "Shows version information.")]
		public bool Version {
			get;
			set;
		}

		/// <summary>
		/// true, um Hilfsinformationen anzuzeigen; andernfalls false.
		/// </summary>
		[CommandLineOption(Name = "h", Aliases = "help",
			Description = "Shows this help text.")]
		public bool Help {
			get;
			set;
		}

		/// <summary>
		/// Der Name der Eingabedatei, die verarbeitet werden soll.
		/// </summary>
		public string InputFile {
			get;
			set;
		}

		/// <summary>
		/// Initialisiert eine neue Instanz der Options-Klasse.
		/// </summary>
		public Options() {
			// Momentan ist nur Pair-Contracting implementiert.
			Algorithm = "PairContract";
		}
	}
}
