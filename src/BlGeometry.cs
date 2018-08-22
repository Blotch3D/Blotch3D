using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blotch
{
	/// <summary>
	/// Methods and helpers for creating various geometric objects
	/// </summary>
	public class BlGeometry
	{
		static Random Rand = new Random();

		/// <summary>
		/// Creates a surface mesh in the form of a triangle list from the height information in an image. The mesh
		/// can be assigned to an element of BlSprite#LODs. You can also concatenate multiple
		/// arrays to create more complex models. Also see #CreateMeshSurface.
		/// </summary>
		/// <param name="tex">The texture that represents the height (Z) of each vertex.</param>
		/// <param name="yScale">Multiplier to apply to the height</param>
		/// <param name="mirrorY">If true, then invert image's Y</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian smoothing kernel, or not</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <param name="numSignificantBits">How many bits in a pixel should be used (starting from the least significant bit).
		/// Normally the first 8 bits are used (the last channel), but special images might combine the bits of multiple channels.</param>
		/// <returns>A 'terrain' from the specified image</returns>
		static public VertexPositionNormalTexture[] CreateMeshSurfaceFromImage(Texture2D tex, double yScale = 1, bool mirrorY = false, bool smooth = true, double noiseLevel = .8, int numSignificantBits = 8)
		{
			var width = tex.Width;
			var height = tex.Height;

			int mask = (int)(Math.Pow(2, numSignificantBits) + .5) - 1;

			var len = width * height;
			var pixels = new int[len];
			tex.GetData(pixels);

			Parallel.For(0, len, (n) =>
			{
				pixels[n] = (pixels[n] & mask);
			});

			return CreateMeshSurface(pixels, width, height, yScale, mirrorY, smooth, yScale * noiseLevel);
		}
		/// <summary>
		/// Creates a surface mesh in the form of a triangle list from an int array of height vlues. The mesh
		/// can be assigned to an element of BlSprite#LODs. You can also concatenate multiple
		/// arrays to create more complex models.
		/// Also see #CreateMeshSurfaceFromImage.
		/// </summary>
		/// <param name="heightMap">A flattened array (in row-major order) of vertex heights</param>
		/// <param name="width">X (row) size</param>
		/// <param name="height">Y (column) size</param>
		/// <param name="yScale">Multiplier to apply to the height</param>
		/// <param name="mirrorY">Whether to invert Y</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian smoothing kernel, or not</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <returns></returns>
		static public VertexPositionNormalTexture[] CreateMeshSurface
		(
			int[] heightMap,
			int width,
			int height,
			double yScale = 1,
			bool mirrorY = false,
			bool smooth = false,
			double noiseLevel = 0
		)
		{
			var vertices = new VertexPositionNormalTexture[width * height];
			var mesh = new VertexPositionNormalTexture[width * height * 6];

			// calc Position and textureCoordinates per vertex
			CalcVerticesAndTexcoords(heightMap, width, height, vertices, yScale, noiseLevel, mirrorY, smooth);

			// calculate each vertex normal from the triangles that vertex participates in.
			CalcNormals(vertices, width, height);

			// create triangles
			VerticesToTriangles(vertices, width, height, mesh);

			return mesh;
		}

		/// <summary>
		///  Given a 2D array of 3D points in a grid, return an array of triangles (where every three vertices is a triangle) 
		/// </summary>
		/// <param name="vertices">A flattened 2D (in row-major order) array of points</param>
		/// <param name="width">The X (row) dimension of the heightmap</param>
		/// <param name="height">The Y (column) dimension of the heightmap</param>
		/// <param name="mesh">Output triangles are put here (must be big enough)</param>
		static public void VerticesToTriangles(
			VertexPositionNormalTexture[] vertices,
			int width,
			int height,
			VertexPositionNormalTexture[] mesh
		)
		{
			Parallel.For(0, width - 1, (x) =>
			{
				Parallel.For(0, height - 1, (y) =>
				{
					var srcOffset = x + y * width;
					var destOffset = (x + y * width) * 6;

					var elem00 = vertices[srcOffset];
					var elem10 = vertices[srcOffset + 1];
					var elem01 = vertices[srcOffset + width];
					var elem11 = vertices[srcOffset + width + 1];

					mesh[destOffset] = elem00;
					mesh[destOffset + 1] = elem11;
					mesh[destOffset + 2] = elem10;
					mesh[destOffset + 3] = elem00;
					mesh[destOffset + 4] = elem01;
					mesh[destOffset + 5] = elem11;
				});
			});
		}

		/// <summary>
		/// Adds normals to an existing 2D array of vertices 
		/// </summary>
		/// <param name="vertices">A flattened 2D array (in row-major order) of vertices</param>
		/// <param name="width">The X (row) dimension</param>
		/// <param name="height">The Y (column) dimension</param>
		static public void CalcNormals(
			VertexPositionNormalTexture[] vertices,
			int width,
			int height
		)
		{
			Parallel.For(0, width, (x) =>
			{
				Parallel.For(0, height, (y) =>
				{
					var ofst = x + width * y;

					var elem00 = vertices[ofst];

					// average of all the normals of the triangles that the vertex is a part of.
					var totalNormal = new Vector3();

					// add the normals from the upper left quad
					if (x > 0 && y < height - 1)
					{
						var middleVertex = vertices[x - 1 + width * (y + 1)];
						var rightVertex = vertices[x + width * (y + 1)];
						var leftVertex = vertices[x - 1 + width * y];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}
					// add the normals from the upper right quad
					if (x < width - 1 && y < height - 1)
					{
						var middleVertex = vertices[x + 1 + width * (y + 1)];
						var rightVertex = vertices[x + 1 + width * y];
						var leftVertex = vertices[x + width * (y + 1)];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}
					// add the normals from the lower left quad
					if (x > 0 && y > 0)
					{
						var middleVertex = vertices[x - 1 + width * (y - 1)];
						var rightVertex = vertices[x - 1 + width * y];
						var leftVertex = vertices[x + width * (y - 1)];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}
					// add the normals from the lower right quad
					if (x < width - 1 && y > 0)
					{
						var middleVertex = vertices[x + 1 + width * (y - 1)];
						var rightVertex = vertices[x + width * (y - 1)];
						var leftVertex = vertices[x + 1 + width * y];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}

					totalNormal.Normalize();
					vertices[ofst].Normal = totalNormal;
				});
			});
		}

		/// <summary>
		/// Calculate vertices and texture coordinates, but not normals, from a specified heightmap int array.
		/// </summary>
		/// <param name="heightMap">A flattened array of 2D heights in row-major order</param>
		/// <param name="width">The X (row) dimension of the heightmap</param>
		/// <param name="height">The Y (column) dimension of the heightmap</param>
		/// <param name="vertices">Flattend (row-major) output array of vertices with their texture coordinates. (Normals are zero)</param>
		/// <param name="heightScale">Multiplier for the height</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <param name="mirrorY">Whether to invert the Y dimension</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian blur on each pixel height</param>
		static public void CalcVerticesAndTexcoords
		(
			int[] heightMap,
			int width,
			int height,
			VertexPositionNormalTexture[] vertices,
			double heightScale = 1,
			double noiseLevel = 0,
			bool mirrorY = false,
			bool smooth = false
		)
		{
			// This local function is called by either the non-parallel or the parallel following code
			// (depending on noiseLevel)
			void pixelProcessor(int x, int y)
			{
				var xNormalized = (float)x / width;
				var yNormalized = (float)y / height;
				var ofst = x + width * y;

				double pixel;

				int ym = y;
				if (mirrorY)
				{
					ym = height - y - 1;
				}

				pixel = heightMap[x + ym * width];

				if (smooth)
				{
					double totalWeight = 1;

					// adjacents
					double weight = .6065;

					// adjacent x
					if (x > 0)
					{
						pixel += weight * heightMap[x - 1 + ym * width];
						totalWeight += weight;
					}
					if (x < width - 1)
					{
						pixel += weight * heightMap[x + 1 + ym * width];
						totalWeight += weight;
					}

					// adjacent y
					if (ym > 0)
					{
						pixel += weight * heightMap[x + (ym - 1) * width];
						totalWeight += weight;
					}
					if (ym < height - 1)
					{
						pixel += weight * heightMap[x + (ym + 1) * width];
						totalWeight += weight;
					}

					// diagonal
					weight = .3679;

					if (x > 0)
					{
						if (ym > 0)
						{
							pixel += weight * heightMap[x - 1 + (ym - 1) * width];
							totalWeight += weight;
						}
						if (ym < height - 1)
						{
							pixel += weight * heightMap[x - 1 + (ym + 1) * width];
							totalWeight += weight;
						}
					}

					if (x < width - 1)
					{
						if (ym > 0)
						{
							pixel += weight * heightMap[x + 1 + (ym - 1) * width];
							totalWeight += weight;
						}
						if (ym < height - 1)
						{
							pixel += weight * heightMap[x + 1 + (ym + 1) * width];
							totalWeight += weight;
						}
					}

					pixel /= totalWeight;
				};

				float pixelHeight = (float)(pixel * heightScale);

				if (noiseLevel != 0)
					pixelHeight += (float)(Rand.NextDouble() * noiseLevel);

				vertices[ofst].Position = new Vector3(xNormalized - .5f, yNormalized - .5f, pixelHeight);
				vertices[ofst].TextureCoordinate = new Vector2(xNormalized, yNormalized);
			};

			if (noiseLevel == 0)
			{
				Parallel.For(0, width, (x) =>
				{
					Parallel.For(0, height, (y) =>
					{
						pixelProcessor(x, y);
					});
				});
			}
			else
			{
				// (Can't be parallel because Random is not re-entrant)
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						pixelProcessor(x, y);
					}
				}
			}
		}
	}
}
