using OpenTK;
using System;
using System.Collections.Generic;

namespace MeshSimplify.Algorithms {
	/// <summary>
	/// Repräsentiert ein Vertexpaar.
	/// </summary>
	/// <remarks>
	/// Pair Instanzen sind sortierbar, wobei aufsteigend nach Kosten sortiert wird,
	/// d.h. die Pair Instanz mit den kleinsten Kosten steht vorne.
	/// </remarks>
	public class Pair : IComparable<Pair> {
		/// <summary>
		/// Der erste Vertex des Paares.
		/// </summary>
		public int Vertex1;

		/// <summary>
		/// Der zweite Vertex des Paares.
		/// </summary>
		public int Vertex2;

		/// <summary>
		/// Die minimalen Kosten für die Kontraktion des Paares.
		/// </summary>
		public double Cost;

		/// <summary>
		/// Die optimale Position für die Kontraktion des Paares.
		/// </summary>
		public Vector3d Target;

		/// <summary>
		/// Initialisiert eine neue Instanz der Pair-Klasse.
		/// </summary>
		/// <param name="v1">
		/// Der erste Vertex des Paares.
		/// </param>
		/// <param name="v2">
		/// Der zweite Vertex des Paares.
		/// </param>
		/// <param name="target">
		/// Die optimale Position für die Kontraktion der beiden Vertices.
		/// </param>
		/// <param name="cost">
		/// Die minimalen Kosten für die Kontraktion der beiden Vertices.
		/// </param>
		public Pair(int v1, int v2, Vector3d target, double cost) {
			Vertex1 = v1;
			Vertex2 = v2;
			Target = target;
			Cost = cost;
		}
		
		/// <summary>
		/// Converts this instance to a string representation.
		/// </summary>
		/// <returns>
		/// A string representation of this instance.
		/// </returns>
		public override string ToString() {
			return "(" + Vertex1 + "," + Vertex2 + ")";
		}

		/// <summary>
		/// Compares this instance to the specified Pair instance.
		/// </summary>
		/// <param name="other">
		/// The Pair instance to compare this instance to.
		/// </param>
		/// <returns>
		/// 1 if this instance is greater than the other instance, or -1 if this instance
		/// is less than the other instance, or 0 if both instances are equal.
		/// </returns>
		public int CompareTo(Pair other) {
			if (Cost > other.Cost)
				return 1;
			if (Cost < other.Cost)
				return -1;
			return 0;
		}
	}
}
