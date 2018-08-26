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
	/// Methods and helpers for creating various geometric objects. These methods create and manage vertex
	/// array meshes (as a flattened row-major VertexPositionNormalTexture[]), triangle arrays (also as a flattened
	/// row-major VertexPositionNormalTexture[]), and VertexBuffers. You can concatenate multiple vertex
	/// arrays to produce one vertex array, and you can concatentate multiple triangle arrays to produce one
	/// triangle array. You can transform either type of array with TransformMesh. You can create facet or
	/// smooth normals. You can set texture (UV) coordinates. You can convert a vertex array to a triangle
	/// array. Finally, you can convert a triangle array to a VertexBuffer suitable for adding to a
	/// BlSprite.LODs field.
	/// </summary>
	public class BlGeometry
	{
		static Random Rand = new Random();

		/// <summary>
		/// Creates a square 1x1 surface in XY but with variation of its Z depending on the pixels in an image (heightfield).
		/// Returns a triangle array. Because the X and Y dimensions of the surface are 1 and because a pixel value of
		/// '1' moves the height up by 1, you will probably want to call TransformMesh on the triangle array so that the
		/// width, depth, and height are more reasonable. Also see #CreatePlanarMeshSurface.
		/// </summary>
		/// <param name="tex">The texture that represents the height (Z) of each vertex.</param>
		/// <param name="mirrorY">If true, then invert image's Y dimension</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian smoothing kernel, or not</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <param name="numSignificantBits">How many bits in a pixel should be used (starting from the least significant bit).
		/// Normally the first 8 bits are used (the last channel), but special images might combine the bits of multiple channels.</param>
		/// <returns>The triangles of a terrain from the specified image</returns>
		static public VertexPositionNormalTexture[] CreatePlanarSurfaceFromImage
		(
			Texture2D tex,
			bool mirrorY = false,
			bool smooth = true,
			double noiseLevel = .8,
			int numSignificantBits = 8
		)
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

			return CreatePlanarSurface(pixels, width, mirrorY, smooth, noiseLevel);
		}
		/// <summary>
		/// Creates a square 1x1 surface in XY but with variation of its Z depending on the elements of an int array of height values.
		/// Returns a triangle array. Because the X and Y dimensions of the surface are 1 and because a heightMap element value of
		/// '1' moves the height up by 1, you will probably want to call TransformMesh on the triangle array so that the
		/// width, depth, and height are more reasonable. Also see #CreateMeshSurfaceFromImage.
		/// numY is assumed to be heightMap.Length/numX.
		/// </summary>
		/// <param name="heightMap">A flattened array (in row-major order) of vertex heights</param>
		/// <param name="numX">The number of X elements in a row</param>
		/// <param name="mirrorY">Whether to invert Y</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian smoothing kernel, or not</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <returns></returns>
		static public VertexPositionNormalTexture[] CreatePlanarSurface
		(
			int[] heightMap,
			int numX,
			bool mirrorY = false,
			bool smooth = false,
			double noiseLevel = 0
		)
		{
			var numY = heightMap.Length / numX;

			if (numY * numX != heightMap.Length)
				throw new Exception("BlGeometry.CreatePlanarSurface: length of heightMap array not divisible by numX");

			// calc Position and textureCoordinates per vertex
			var mesh = CalcPlanarVerticesAndTexcoords(heightMap, numX, noiseLevel, mirrorY, smooth);

			// calculate each vertex normal from the adjacent vertices.
			CalcSmoothMeshNormals(mesh, numX);

			// create triangles
			var triangles = VerticesToTriangles(mesh, numX);

			return triangles;
		}

		/// <summary>
		/// Creates a cylindroid (including texture coords and normals) with the given parameters, and returns
		/// a triangle array. Assuming a possible subsequent call to TransformMesh, many fundamental rotationally
		/// symmetric shapes can be generated, like a cylinder, cone, washer, disk, prism of any number of facets,
		/// tetrahedron, pyramid of any number of facets, etc. If no heightMap is specified and before passing the
		/// result to TransformMesh, the center of the cylindroid is the origin, its height is 1, the diameter of
		/// the base is 1, and the diameter of the top is topDiameter. If heightMap is specified, it is mapped onto
		/// the object such that the heightMap X wraps around horizontally and the heightMap Y maps to the height (Z)
		/// of the object. A corresponding heightMap element divided by 1e4 is added to the parameterized diameter
		/// at the corresponding point. For example, if the topDiameter is equal to 2, then the parameterized
		/// diameter without a heightField at the halfway point is 1.5 (half way between a bottom diameter of 1 and
		/// top diameter of 2), but if the corresponding heightField value at that position is -2e3, then the
		/// diameter at that point is 1.5 + -2e3/1e4 = 1.3. Multiple triangle arrays can be concatenated. For some
		/// shapes you may also want to re-calculate normals with CalcFacetNormals (for example, if the the
		/// sunsequent transform caused some normals to become invalid), and/or use ScaleNormals method to
		/// invert them, where needed.
		/// </summary>
		/// <param name="numHorizVertices">The number of horizontal vertices in a row</param>
		/// <param name="numVertVertices">The number of vertical vertices in a column</param>
		/// <param name="topDiameter">Diameter of top of cylindroid (if heightMap==null)</param>
		/// <param name="facetedNormals">If true, create normals per triangle. If false, create smooth normals</param>
		/// <param name="heightMap">If not null, then this is mapped onto the surface to modify the diameter. See method
		/// description for details. This must be a flattened (row-major) array and must have the dimensions
		/// numHorizVertices x numVertVertices.</param>
		/// <returns>A triangle list of the cylindroid</returns>
		static public VertexPositionNormalTexture[] CreateCylindroidMeshSurface
		(
			int numHorizVertices=32,
			int numVertVertices=2,
			double topDiameter=1,
			bool facetedNormals=false,
			int[] heightMap=null
		)
		{
			numHorizVertices++;
			// calc Position and textureCoordinates per vertex
			var mesh = CalcCylindroidVerticesAndTexcoords(numHorizVertices, numVertVertices, topDiameter, heightMap);

			if(!facetedNormals)
			{
				// calculate each vertex normal from the adjacent vertices.
				mesh = CalcSmoothMeshNormals(mesh, numHorizVertices, true);
			}

			// create triangles
			var triangles = VerticesToTriangles(mesh, numHorizVertices);

			if (facetedNormals)
			{
				// calculate each vertex normal from the associated triangle.
				triangles = CalcFacetNormals(triangles);
			}


			return triangles;
		}

		/// <summary>
		/// Like CreateCylindroidMeshSurface, but returns the vertices rather than a triangle list, and doesn't calculate the
		/// normals, so you'll need to do that separately with the appropriate functions.
		/// </summary>
		/// <param name="numX">The number of X elements in a row</param>
		/// <param name="numY">The number of Y elements in a column</param>
		/// <param name="topDiameter">Diameter of top of cylindroid (if heightMap==null)</param>
		/// <param name="heightMap">See CreateCylindroidMeshSurface</param>
		/// <returns>A list of the cylindroid's vertices</returns>
		static public VertexPositionNormalTexture[] CalcCylindroidVerticesAndTexcoords
		(
			int numX=32,
			int numY=2,
			double topDiameter=1,
			int[] heightMap = null
		)
		{
			var mesh = new VertexPositionNormalTexture[numX * numY];
			for (int x=0;x<numX;x++)
			{
				for (int y = 0; y < numY; y++)
				{
					var zord = (double)y / (numY-1);

					var diameter = (1 - zord) + zord * topDiameter;
					
					diameter *= .5;

					if(heightMap!=null)
					{
						diameter += heightMap[x + y * numX]/1e4;
					}

					var offsetX = (double)x / (numX-1);
					var angle = offsetX * Math.PI * 2;

					var xord = Math.Sin(angle) * diameter;

					double yord;
					yord = -Math.Cos(angle) * diameter;

					var i = x + y * numX;
					mesh[i].Position.X = (float)xord;
					mesh[i].Position.Y = (float)yord;
					mesh[i].Position.Z = (float)zord-.5f;

					mesh[i].TextureCoordinate.X = (float)offsetX;
					mesh[i].TextureCoordinate.Y = (float)zord;
				}
			}
			return mesh;
		}

		/// <summary>
		///  Given a regular 2D array of vertices, return an array of triangles.
		///  numY is assumed to be the length of vertices/numX.
		/// </summary>
		/// <param name="vertices">A flattened 2D (in row-major order) array of points</param>
		/// <param name="numX">The number of X elements in a row</param>
		/// <returns>Triangle mesh</returns>
		static public VertexPositionNormalTexture[] VerticesToTriangles(
			VertexPositionNormalTexture[] vertices,
			int numX
		)
		{
			var numY = vertices.Length / numX;

			if (numY * numX != vertices.Length)
				throw new Exception("BlGeometry.VerticesToTriangles: length of vertices array not divisible by numX");

			// Allocate triangle array
			var triangles = new VertexPositionNormalTexture[(numX - 1) * (numY - 1) * 6];

			Parallel.For(0, numX - 1, (x) =>
			{
				Parallel.For(0, numY - 1, (y) =>
				{
					var srcOffset = x + y * numX;
					var destOffset = (x + y * (numX-1)) * 6;

					var elem00 = vertices[srcOffset];
					var elem10 = vertices[srcOffset + 1];
					var elem01 = vertices[srcOffset + numX];
					var elem11 = vertices[srcOffset + numX + 1];

					triangles[destOffset] = elem00;
					triangles[destOffset + 1] = elem11;
					triangles[destOffset + 2] = elem10;
					triangles[destOffset + 3] = elem00;
					triangles[destOffset + 4] = elem01;
					triangles[destOffset + 5] = elem11;
				});
			});
			return triangles;
		}

		/// <summary>
		/// Given a triangle list, returns a VertexBuffer
		/// </summary>
		/// <param name="graphicsDevice">The graphics device to use</param>
		/// <param name="vertices">The triangles to convert to a VertexBuffer</param>
		/// <returns>The VertexBuffer that contains the triangle list</returns>
		static public VertexBuffer TrianglesToVertexBuffer(GraphicsDevice graphicsDevice, VertexPositionNormalTexture[] vertices)
		{
			var vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertices.Length, BufferUsage.WriteOnly);
			vertexBuffer.SetData(vertices);
			return vertexBuffer;
		}
		/// <summary>
		/// Transforms the vertices of the mesh (including transforming the transpose of the inverse of each normal) according
		/// to the specified matrix.
		/// You can pass an array of points or an array of triangles.
		/// If its an array of triangles, you will probably want to call CullEmptyTriangles (if the transform might cause some triangles
		/// to have zero area) and maybe CalcSmoothNormals or
		/// CalcFaceNormals afterward, because transforming can sometimes cause normals to be invalid or point the wrong way, causing the
		/// surface to be black or the wrong brightness.
		/// </summary>
		/// <param name="mesh">Input mesh (This is altered by the method)</param>
		/// <param name="matrix">Transformation matrix</param>
		/// <returns>The transformed mesh</returns>
		static public VertexPositionNormalTexture[] TransformMesh(VertexPositionNormalTexture[] mesh, Matrix matrix)
		{
			var len = mesh.Length;
			Parallel.For(0, len, (n) =>
			{
				mesh[n].Position = Vector3.Transform(mesh[n].Position, matrix);
				mesh[n].Normal = Vector3.Transform(mesh[n].Normal, Matrix.Transpose(Matrix.Invert(matrix)));
			});

			return mesh;
		}

		/// <summary>
		/// Removes triangles that have zero area. Typically called after a transform.
		/// </summary>
		/// <param name="triangles">The input triangles (i.e. NOT a regular mesh</param>
		/// <returns>Output triangles</returns>
		static public VertexPositionNormalTexture[] CullEmptyTriangles(VertexPositionNormalTexture[] triangles)
		{
			var newList = new List<VertexPositionNormalTexture>();

			var len = triangles.Length;

			var numTris = len / 3;
			if (numTris * 3 != triangles.Length)
				throw new Exception("BlGeometry.TransformMesh expected triangles, but the number of vertices are not divisible by three.");

			for (int n = 0; n < numTris; n++)
			{
				var m = n * 3;

				if (triangles[m].Position != triangles[m + 1].Position && triangles[m].Position != triangles[m + 2].Position && triangles[m + 1].Position != triangles[m + 2].Position)
				{
					newList.Add(triangles[m]);
					newList.Add(triangles[m + 1]);
					newList.Add(triangles[m + 2]);
				}
			}

			len = newList.Count;

			var newMesh = new VertexPositionNormalTexture[len];

			for (int n = 0; n < len; n++)
			{
				newMesh[n] = newList[n];
			}
			return newMesh;
		}

		/// <summary>
		/// For a regular mesh (i.e. NOT triangles), calculates a normal for each point in the mesh. The normal for a given point
		/// is an average of the normals of the (typically eight) triangles that the vertex would participates in. (The
		/// triangles have not yet been separated-out.)
		/// numY is assumed to be vertices.Length/numX.
		/// </summary>
		/// <param name="vertices">A flattened (in row-major order) 2D array of vertices (this method may change the contents of this mesh)</param>
		/// <param name="numX">The number of X elements in a row</param>
		/// <param name="xIsWrapped">Include the row-wrapped ponts in the calculation of normals on the row edge.
		/// Closed cylindroids where x is wrapped would need this.</param>
		/// <param name="invert">Inverts the normals (typically when viewing faces from the inside)</param>
		/// <returns>The input mesh with smooth normals added</returns>
		static public VertexPositionNormalTexture[] CalcSmoothMeshNormals(
			VertexPositionNormalTexture[] vertices,
			int numX,
			bool xIsWrapped=false,
			bool invert=false
		)
		{
			var numY = vertices.Length / numX;

			if (numY * numX != vertices.Length)
				throw new Exception("BlGeometry.CalcSmoothMeshNormals: length of vertices array not divisible by numX");

			//for(int x=0;x<numX;x++)
			Parallel.For(0, numX, (x) =>
			{
				//for (int y = 0; y < numY; y++)
				Parallel.For(0, numY, (y) =>
				{
					var ofst = x + numX * y;

					var elem00 = vertices[ofst];

					// average of all the normals of the triangles that the vertex is a part of.
					var totalNormal = new Vector3();

					var xMinus1 = x - 1;
					var xPlus1 = x + 1;

					if(xIsWrapped)
					{
						if (xMinus1 < 0)
							xMinus1 = numX - 2;
						if (xPlus1 > numX-1)
							xPlus1 = 1;
					}

					// add the normals from the upper left quad
					if ((x > 0 || xIsWrapped) && y < numY - 1)
					{
						var middleVertex = vertices[xMinus1 + numX * (y + 1)];
						var rightVertex = vertices[x + numX * (y + 1)];
						var leftVertex = vertices[xMinus1 + numX * y];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}
					// add the normals from the upper right quad
					if ((x < numX - 1 || xIsWrapped) && y < numY - 1)
					{
						var middleVertex = vertices[xPlus1 + numX * (y + 1)];
						var rightVertex = vertices[xPlus1 + numX * y];
						var leftVertex = vertices[x + numX * (y + 1)];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}
					// add the normals from the lower left quad
					if ((x > 0 || xIsWrapped) && y > 0)
					{
						var middleVertex = vertices[xMinus1 + numX * (y - 1)];
						var rightVertex = vertices[xMinus1 + numX * y];
						var leftVertex = vertices[x + numX * (y - 1)];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}
					// add the normals from the lower right quad
					if ((x < numX - 1 || xIsWrapped) && y > 0)
					{
						var middleVertex = vertices[xPlus1 + numX * (y - 1)];
						var rightVertex = vertices[x + numX * (y - 1)];
						var leftVertex = vertices[xPlus1 + numX * y];

						var rightVector = rightVertex.Position - elem00.Position;
						var middleVector = middleVertex.Position - elem00.Position;
						var leftVector = leftVertex.Position - elem00.Position;

						totalNormal += Vector3.Cross(rightVector, middleVector);
						totalNormal += Vector3.Cross(middleVector, leftVector);
					}

					totalNormal.Normalize();

					if (invert)
						totalNormal = -totalNormal;

					vertices[ofst].Normal = totalNormal;
				});
			});
			return vertices;
		}

		/// <summary>
		/// Scales the normals. Inverts the normals by default.
		/// </summary>
		/// <param name="vertices">Input array of vertices, and output array as well</param>
		/// <param name="scale">Scales applied to each normal</param>
		static public VertexPositionNormalTexture[] ScaleNormals(
			VertexPositionNormalTexture[] vertices,
			double scale = -1
		)
		{
			var s = (float)scale;
			Parallel.For(0, vertices.Length, (n) =>
			{
				vertices[n].Normal = vertices[n].Normal * s;
			});
			return vertices;
		}

		/// <summary>
		/// Set texture coordinates (UV) to normalized XY planar.
		/// </summary>
		/// <param name="vertices">Input array of vertices, and output array as well</param>
		static public VertexPositionNormalTexture[] SetTextureToXY(
			VertexPositionNormalTexture[] vertices
		)
		{
			float minX = 1e30f;
			float maxX = -1e30f;
			float minY = 1e30f;
			float maxY = -1e30f;

			var len = vertices.Length;
			for(int n = 0; n<len;n++)
			{
				var pos = vertices[n].Position;
				if (minX > pos.X)
					minX = pos.X;
				if (minY > pos.Y)
					minY = pos.Y;
				if (maxX < pos.X)
					maxX = pos.X;
				if (maxY < pos.Y)
					maxY = pos.Y;
			}

			float rngX = maxX - minX;
			float rngY = maxY - minY;

			Parallel.For(0, vertices.Length, (n) =>
			{
				var pos = vertices[n].Position;
				vertices[n].TextureCoordinate.X = (pos.X - minX) * rngX;
				vertices[n].TextureCoordinate.Y = (pos.Y - minY) * rngY;
			});
			return vertices;
		}

		/// <summary>
		/// Calculates one normal for each triangle in an existing 2D array of triangles (NOT a regular grid of points). The normal for each
		/// triangle is orthogonal to its surface.
		/// </summary>
		/// <param name="vertices">A flattened (in row-major order) 2D array of triangles (this array is changed to be the putput array)</param>
		/// <param name="invert">Inverts the normals (typically when viewing faces from the inside)</param>
		/// <returns>Array with normals calculated</returns>
		static public VertexPositionNormalTexture[] CalcFacetNormals(
			VertexPositionNormalTexture[] vertices,
			bool invert = false
		)
		{
			int len = vertices.Length/3;

			if (len * 3 != vertices.Length)
				throw new Exception("BlGeometry.CalcFaceNormals expected triangles, but length of input array is not divisible by three");

			//for(int n=0;n<len;n++)
			Parallel.For(0, len, (n) =>
			{
				var m = n * 3;

				var middleVertex = vertices[m];
				var rightVertex = vertices[m +1];
				var leftVertex = vertices[m +2];

				var rightVector = middleVertex.Position - rightVertex.Position;
				var leftVector = middleVertex.Position - leftVertex.Position;

				var normal = Vector3.Cross(leftVector, rightVector);

				if (invert)
					normal = -normal;

				normal.Normalize();
				vertices[m].Normal = normal;
				vertices[m + 1].Normal = normal;
				vertices[m + 2].Normal = normal;
			});
			return vertices;
		}

		/// <summary>
		/// Calculate vertices and texture coordinates, but not normals, from a specified heightmap int array.
		/// numY is assumed to be heightMap.Length/numX.
		/// </summary>
		/// <param name="heightMap">A flattened array of 2D heights in row-major order</param>
		/// <param name="numX">The number of X elements in a row</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <param name="mirrorY">Whether to invert the Y dimension</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian blur on each pixel height</param>
		static public VertexPositionNormalTexture[] CalcPlanarVerticesAndTexcoords
		(
			int[] heightMap,
			int numX,
			double noiseLevel = 0,
			bool mirrorY = false,
			bool smooth = false
		)
		{
			var numY = heightMap.Length / numX;

			if (numY * numX != heightMap.Length)
				throw new Exception("BlGeometry.CalcPlanarVerticesAndTexcoords: length of heightMap array not divisible by numX");

			var vertices = new VertexPositionNormalTexture[numX * numY];

			// This local function is called by either the non-parallel or the parallel following code
			// (depending on noiseLevel)
			void pixelProcessor(int x, int y)
			{
				var xNormalized = (float)x / numX;
				var yNormalized = (float)y / numY;
				var ofst = x + numX * y;

				double pixel;

				int ym = y;
				if (mirrorY)
				{
					ym = numY - y - 1;
				}

				pixel = heightMap[x + ym * numX];

				if (smooth)
				{
					double totalWeight = 1;

					// adjacents
					double weight = .6065;

					// adjacent x
					if (x > 0)
					{
						pixel += weight * heightMap[x - 1 + ym * numX];
						totalWeight += weight;
					}
					if (x < numX - 1)
					{
						pixel += weight * heightMap[x + 1 + ym * numX];
						totalWeight += weight;
					}

					// adjacent y
					if (ym > 0)
					{
						pixel += weight * heightMap[x + (ym - 1) * numX];
						totalWeight += weight;
					}
					if (ym < numY - 1)
					{
						pixel += weight * heightMap[x + (ym + 1) * numX];
						totalWeight += weight;
					}

					// diagonal
					weight = .3679;

					if (x > 0)
					{
						if (ym > 0)
						{
							pixel += weight * heightMap[x - 1 + (ym - 1) * numX];
							totalWeight += weight;
						}
						if (ym < numY - 1)
						{
							pixel += weight * heightMap[x - 1 + (ym + 1) * numX];
							totalWeight += weight;
						}
					}

					if (x < numX - 1)
					{
						if (ym > 0)
						{
							pixel += weight * heightMap[x + 1 + (ym - 1) * numX];
							totalWeight += weight;
						}
						if (ym < numY - 1)
						{
							pixel += weight * heightMap[x + 1 + (ym + 1) * numX];
							totalWeight += weight;
						}
					}

					pixel /= totalWeight;
				};

				float pixelHeight = (float)pixel;

				if (noiseLevel != 0)
					pixelHeight += (float)(Rand.NextDouble() * noiseLevel);

				vertices[ofst].Position = new Vector3(xNormalized - .5f, yNormalized - .5f, pixelHeight);
				vertices[ofst].TextureCoordinate = new Vector2(xNormalized, yNormalized);
			};

			if (noiseLevel == 0)
			{
				Parallel.For(0, numX, (x) =>
				{
					Parallel.For(0, numY, (y) =>
					{
						pixelProcessor(x, y);
					});
				});
			}
			else
			{
				// (Can't be parallel because Random is not re-entrant)
				for (int x = 0; x < numX; x++)
				{
					for (int y = 0; y < numY; y++)
					{
						pixelProcessor(x, y);
					}
				}
			}
			return vertices;
		}
	}
}
