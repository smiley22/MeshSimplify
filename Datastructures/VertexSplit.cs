using OpenTK;
using System.Collections.Generic;

namespace MeshSimplify {
	/// <summary>
	/// Repräsentiert eine Vertex-Split Operation, also das Gegenteil einer Edge- bzw.
	/// Pair-Contract operation.
	/// </summary>
	public class VertexSplit {
		/// <summary>
		/// Der Index des Vertex, der "gespalten" wird.
		/// </summary>
		public int S;

		/// <summary>
		/// Die neue Position des Vertex nach der Aufspaltung.
		/// </summary>
		public Vector3d SPosition;

		/// <summary>
		/// Die Position des zweiten Vertex der bei der Aufspaltung entsteht.
		/// </summary>
		public Vector3d TPosition;

		/// <summary>
		/// Eine Untermenge der Facetten von Vertex s die Vertex t nach der Aufspaltung
		/// zugeteilt werden sollen.
		/// </summary>
		public ISet<Triangle> Faces = new HashSet<Triangle>();
	}
}
