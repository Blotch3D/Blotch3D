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
	/// Methods and helpers for creating various geometric objects. These methods create and manage regular
	/// grids of vertices as a flattened column-major VertexPositionNormalTexture[], triangle arrays
	/// (also as a VertexPositionNormalTexture[]), and VertexBuffers. You can concatenate
	/// multiple regular grids to produce one regular grid if they have the same number of columns, and you can
	/// concatentate multiple triangle arrays to produce one triangle array. You can transform either type of
	/// array with TransformVertices. You can create smooth normals for a regular grid and facet normals for a
	/// triangle array. You can set texture (UV) coordinates. You can convert a regular grid to a triangle
	/// array. Finally, you can convert a triangle array to a VertexBuffer suitable for adding to a
	/// BlSprite.LODs field.
	/// </summary>
	public class BlGeometry
	{
		static Random Rand = new Random();

		/// <summary>
		/// Creates a square 1x1 surface in XY but with variation of its Z depending on the pixels in an image (heightfield).
		/// A maximum pixel value causes the corresponding position on the surface to have a height of 1. Use TransformVertices
		/// to alter this.
		/// Returns a triangle array of the surface, which includes smooth normals and texture coordinates.
		/// </summary>
		/// <param name="tex">The texture that represents the height (Z) of each vertex.</param>
		/// <param name="mirrorY">If true, then invert image's Y dimension</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian smoothing kernel, or not</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <param name="numSignificantBits">How many bits in a pixel should be used (starting from the least significant bit).
		/// Normally the first 8 bits are used (the last channel), but special images might combine the bits of multiple channels.</param>
		/// <returns>The triangles of a terrain from the specified image, including smooth normals and texture coordinates</returns>
		static public VertexPositionNormalTexture[] CreatePlanarSurface
		(
			Texture2D tex,
			bool mirrorY = false,
			bool smooth = true,
			double noiseLevel = 1.0/256,
			int numSignificantBits = 8
		)
		{
			var width = tex.Width;
			var height = tex.Height;

			int mask = (int)(Math.Pow(2, numSignificantBits) + .5) - 1;

			var len = width * height;
			var pixels = new int[len];
			tex.GetData(pixels);

			var heightMap = new double[height, width];

			Parallel.For(0, width, (x) =>
			{
				Parallel.For(0, height, (y) =>
				{
					// (pixels are row-major, heightMap is column-major)
					heightMap[y,x] = (double)(pixels[x + width*y] & mask)/(mask+1);
				});
			});

			return CreatePlanarSurface(heightMap, mirrorY, smooth, noiseLevel);
		}

		/// <summary>
		/// The delegate passed to certain geometry methods. Given an X and Y value, return a Z value.
		/// </summary>
		/// <param name="x">The x of the surface position</param>
		/// <param name="y">The y of the surface position</param>
		/// <returns>The height or diameter multiplier of the surface at the corresponding XY position</returns>
		public delegate double XYToZDelegate(int x, int y);

		/// <summary>
		/// Creates a square 1x1 surface in XY but with variation of its Z depending on the
		/// result of a delegate.
		/// Returns a triangle array of the surface, which includes smooth normals and texture coordinates.
		/// </summary>
		/// <param name="pixelFunc">A delegate that takes the x and y and returns that pixel's height</param>
		/// <param name="numX">The number of X elements in a row</param>
		/// <param name="numY">The number of X elements in a row</param>
		/// <param name="mirrorY">Whether to invert Y</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian smoothing kernel, or not</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <returns>Triangles of the surface, including smooth normals and teture coordinates</returns>
		static public VertexPositionNormalTexture[] CreatePlanarSurface
		(
			XYToZDelegate pixelFunc,
			int numX = 256,
			int numY = 256,
			bool mirrorY = false,
			bool smooth = false,
			double noiseLevel = 0
		)
		{
			var heightMap = new double[numY, numX];

			Parallel.For(0, numX, (x) =>
			{
				Parallel.For(0, numY, (y) =>
				{
					heightMap[y, x] = pixelFunc(x, y);
				});
			});

			return CreatePlanarSurface(heightMap, mirrorY, smooth, noiseLevel);
		}



		/// <summary>
		/// Creates a square 1x1 surface in XY but with variation of its Z depending on the
		/// elements of a 2D array of doubles.
		/// Returns a triangle array of the surface, which includes smooth normals and texture coordinates.
		/// </summary>
		/// <param name="heightMap">A flattened array (in column-major order) of vertex heights. (Note that
		/// this means the 2D form is [y, x], because rows are the second index in C#)</param>
		/// <param name="mirrorY">Whether to invert Y</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian smoothing kernel, or not</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <returns>Triangles of the surface, including smooth normals and teture coordinates</returns>
		static public VertexPositionNormalTexture[] CreatePlanarSurface
		(
			double[,] heightMap,
			bool mirrorY = false,
			bool smooth = false,
			double noiseLevel = 0
		)
		{
			// calc Position and textureCoordinates per vertex
			var grid = CalcPlanarVerticesAndTexcoords(heightMap, noiseLevel, mirrorY, smooth);

			var width = heightMap.GetLength(1);

			// calculate each vertex normal from the adjacent vertices.
			CalcSmoothNormals(grid, width);

			// create triangles
			var triangles = VerticesToTriangles(grid, width);

			return triangles;
		}

		/// <summary>
		/// Like the #CreateCylindroidSurface overload that takes a heightMap (see that method for details),
		/// but this takes a delegate that defines the diameter multiplier, instead.
		/// </summary>
		/// <param name="pixelFunc">A delegate that takes an x and y and returns the diameter multiplier</param>
		/// <param name="numHorizVertices">The number of horizontal vertices in a row</param>
		/// <param name="numVertVertices">The number of vertical vertices in a column</param>
		/// <param name="topDiameter">Diameter of top of cylindroid (if heightMap==null)</param>
		/// <param name="facetedNormals">If true, create normals per triangle. If false, create smooth normals</param>
		/// <param name="endCaps">Whether to also create a cap for each end</param>
		/// <returns></returns>
		static public VertexPositionNormalTexture[] CreateCylindroidSurface
		(
			XYToZDelegate pixelFunc,
			int numHorizVertices = 32,
			int numVertVertices = 2,
			double topDiameter = 1,
			bool facetedNormals = false,
			bool endCaps = false
		)
		{
			var heightMap = new double[numVertVertices, numHorizVertices];

			Parallel.For(0, numHorizVertices, (x) =>
			{
				Parallel.For(0, numVertVertices, (y) =>
				{
					heightMap[y, x] = pixelFunc(x, y);
				});
			});

			return CreateCylindroidSurface(numHorizVertices, numVertVertices, topDiameter, facetedNormals, heightMap, endCaps);
		}

		/// <summary>
		/// Creates a cylindroid (including texture coords and normals) with the given parameters, and returns
		/// a triangle array, which includes smooth normals and texture coordinates. Assuming a possible subsequent
		/// call to #TransformVertices, even without a heightMap many
		/// fundamental rotationally symmetric shapes can be generated, like a cylinder, cone, washer, disk, prism
		/// of any number of facets, tetrahedron, pyramid of any number of facets, etc. Before passing the result
		/// to #TransformVertices, the center of the cylindroid is the
		/// origin, its height is 1, the diameter of the base is 1, and the diameter of the top is topDiameter. If
		/// heightMap is specified, it multiplies the parameterized diameter at multiple points on the surface. The dimensions of
		/// heightMap can be different from the dimensions of the cylindroid. (Note that in C#, the second index of a 2D array
		/// is the rows. For example, [y, x].) heightMap is mapped onto the object
		/// such that the heightMap X wraps around horizontally and the heightMap Y is mapped vertically to the
		/// height (Z) of the object. For example, if the heightMap X dimension is 1, then it defines the diameter
		/// shape that is rotated around the whole cylindroid. For some shapes you may also want to
		/// re-calculate normals with #CalcFacetNormals (for example, if the the subsequent transform caused some
		/// normals to become invalid). See the GeomObjects examples.
		/// </summary>
		/// <param name="numHorizVertices">The number of horizontal vertices in a row</param>
		/// <param name="numVertVertices">The number of vertical vertices in a column</param>
		/// <param name="topDiameter">Diameter of top of cylindroid (if heightMap==null)</param>
		/// <param name="facetedNormals">If true, create normals per triangle. If false, create smooth normals</param>
		/// <param name="heightMap">If not null, then this is mapped onto the surface to modify the diameter. See method
		/// description for details. This need not have the same dimensions as the cylindroid.</param>
		/// <param name="endCaps">Whether to also create a cap for each end</param>
		/// <returns>A triangle list of the cylindroid</returns>
		static public VertexPositionNormalTexture[] CreateCylindroidSurface
		(
			int numHorizVertices=32,
			int numVertVertices=2,
			double topDiameter=1,
			bool facetedNormals=false,
			double[,] heightMap=null,
			bool endCaps = false
		)
		{
			numHorizVertices++;

			// calc Position and textureCoordinates per vertex
			var grid = CalcCylindroidVerticesAndTexcoords(numHorizVertices, numVertVertices, topDiameter, heightMap);

			if(!facetedNormals)
			{
				// calculate each vertex normal from the adjacent vertices.
				grid = CalcSmoothNormals(grid, numHorizVertices, true);
			}

			// create triangles
			var triangles = VerticesToTriangles(grid, numHorizVertices);

			if (facetedNormals)
			{
				// calculate each vertex normal from the associated triangle.
				triangles = CalcFacetNormals(triangles);
			}

			if(endCaps)
			{
				if (endCaps & heightMap != null)
				{
					if(topDiameter != 0)
					{
						int columns = heightMap.GetLength(0);
						int rows = heightMap.GetLength(1);

						double[,] topMap = new double[1, rows];

						Buffer.BlockCopy(heightMap, sizeof(double) * rows * (columns - 1), topMap, 0, sizeof(double) * rows);

						var topGrid = CalcCylindroidVerticesAndTexcoords(numHorizVertices, 2, 0, topMap);

						// create triangles
						var topTriangles = VerticesToTriangles(topGrid, numHorizVertices);

						topTriangles = TransformVertices(topTriangles, Matrix.CreateScale(1, 1, 0));

						// calculate each vertex normal from the associated triangle.
						topTriangles = CalcFacetNormals(topTriangles);

						
					}
				}
			}

			return triangles;
		}

		/// <summary>
		/// Like #CreateCylindroidSurface, but returns a regular grid rather than a triangle list, and
		/// doesn't calculate the normals, so you'll need to do that separately with the appropriate functions.
		/// </summary>
		/// <param name="numX">The number of X elements in a row</param>
		/// <param name="numY">The number of Y elements in a column</param>
		/// <param name="topDiameter">Diameter of top of cylindroid (if heightMap==null)</param>
		/// <param name="heightMap">See CreateCylindroidSurface</param>
		/// <returns>A list of the cylindroid's vertices</returns>
		static public VertexPositionNormalTexture[] CalcCylindroidVerticesAndTexcoords
		(
			int numX = 32,
			int numY = 2,
			double topDiameter = 1,
			double[,] heightMap = null
		)
		{
			double hmXRatio = 0;
			double hmYRatio = 0;
			if (heightMap != null)
			{
				hmXRatio = heightMap.GetLength(1) / (double)(numX-1);
				hmYRatio = heightMap.GetLength(0) / (double)numY;
			}

			var grid = new VertexPositionNormalTexture[numX * numY];
			for (int x=0;x<numX;x++)
			{
				for (int y = 0; y < numY; y++)
				{
					var zord = (double)y / (numY-1);

					var radius = (1 - zord) + zord * topDiameter;
					
					radius *= .5;

					if(heightMap!=null)
					{
						int hx = (int)(x * hmXRatio);
						int hy = (int)(y * hmYRatio);
						radius *= heightMap[hy, hx % heightMap.GetLength(1)];
					}

					var offsetX = (double)x / (numX-1);
					var angle = offsetX * Math.PI * 2;

					var xord = Math.Sin(angle) * radius;

					double yord;
					yord = -Math.Cos(angle) * radius;

					var i = x + y * numX;
					grid[i].Position.X = (float)xord;
					grid[i].Position.Y = (float)yord;
					grid[i].Position.Z = (float)zord-.5f;

					grid[i].TextureCoordinate.X = (float)offsetX;
					grid[i].TextureCoordinate.Y = (float)zord;
				}
			}
			return grid;
		}

		/// <summary>
		///  Given a regular grid of vertices, return an array of triangles.
		///  numY is assumed to be the length of vertices/numX.
		/// </summary>
		/// <param name="vertices">A flattened 2D (in column-major order) array of points</param>
		/// <param name="numX">The number of X elements in a row</param>
		/// <returns>Triangle array</returns>
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
		/// Transforms a regular grid or triangle array (including transforming the transpose of the inverse of each
		/// normal) according to the specified matrix.
		/// If its an array of triangles, you will probably want to call #CullEmptyTriangles if the transform might
		/// cause some triangles to have zero area, and maybe #CalcSmoothNormals (for regular grids) or #CalcFaceNormals
		/// (for tiangles) afterward if the transform might cause normals to be invalid or point the wrong
		/// way, causing the surface to be black or the wrong brightness (typically when a dimenson is scaled to zero).
		/// </summary>
		/// <param name="vertices">Input array (this is altered by the method)</param>
		/// <param name="matrix">Transformation matrix</param>
		/// <returns>The transformed array</returns>
		static public VertexPositionNormalTexture[] TransformVertices(VertexPositionNormalTexture[] vertices, Matrix matrix)
		{
			var len = vertices.Length;
			Parallel.For(0, len, (n) =>
			{
				vertices[n].Position = Vector3.Transform(vertices[n].Position, matrix);
				vertices[n].Normal = Vector3.Transform(vertices[n].Normal, Matrix.Transpose(Matrix.Invert(matrix)));
			});

			return vertices;
		}

		/// <summary>
		/// Removes triangles that have zero area. Typically called after a transform.
		/// </summary>
		/// <param name="triangles">The input triangles (i.e. NOT a regular grid)</param>
		/// <returns>Output triangles</returns>
		static public VertexPositionNormalTexture[] CullEmptyTriangles(VertexPositionNormalTexture[] triangles)
		{
			var newList = new List<VertexPositionNormalTexture>();

			var len = triangles.Length;

			var numTris = len / 3;
			if (numTris * 3 != triangles.Length)
				throw new Exception("BlGeometry.TransformVertices expected triangles, but the number of vertices are not divisible by three.");

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

			var newTriangles = new VertexPositionNormalTexture[len];

			for (int n = 0; n < len; n++)
			{
				newTriangles[n] = newList[n];
			}
			return newTriangles;
		}

		/// <summary>
		/// For a regular grid (i.e. NOT triangles), calculates a normal for each point in the grid. The normal for a given point
		/// is an average of the normals of the (typically eight) triangles that the vertex would participates in. (The
		/// triangles have not yet been separated-out.)
		/// numY is assumed to be vertices.Length/numX.
		/// </summary>
		/// <param name="vertices">A flattened (in column-major order) 2D array of vertices (this method may change the contents of this grid)</param>
		/// <param name="numX">The number of X elements in a row</param>
		/// <param name="xIsWrapped">Include the row-wrapped ponts in the calculation of normals on the row edge.
		/// Closed cylindroids where x is wrapped would need this.</param>
		/// <param name="invert">Inverts the normals (typically when viewing faces from the inside)</param>
		/// <returns>The input grid with smooth normals added</returns>
		static public VertexPositionNormalTexture[] CalcSmoothNormals(
			VertexPositionNormalTexture[] vertices,
			int numX,
			bool xIsWrapped=false,
			bool invert=false
		)
		{
			var numY = vertices.Length / numX;

			if (numY * numX != vertices.Length)
				throw new Exception("BlGeometry.CalcSmoothNormals: length of vertices array not divisible by numX");

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
		/// Scales the normals.
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
		/// <param name="vertices">A flattened (in column-major order) 2D array of triangles (this array is changed to be the putput array)</param>
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
		/// Returns a 1x1 surface in XY, but with Z for a given position equal to the corresponding heightMap element.
		/// </summary>
		/// <param name="heightMap">A flattened array of 2D heights in column-major order</param>
		/// <param name="noiseLevel">How much noise to add</param>
		/// <param name="mirrorY">Whether to invert the Y dimension</param>
		/// <param name="smooth">Whether to apply a 3x3 gaussian blur on each pixel height</param>
		static public VertexPositionNormalTexture[] CalcPlanarVerticesAndTexcoords
		(
			double[,] heightMap,
			double noiseLevel = 0,
			bool mirrorY = false,
			bool smooth = false
		)
		{
			var numX = heightMap.GetLength(0);
			var numY = heightMap.GetLength(1);

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

				pixel = heightMap[ym, x];

				if (smooth)
				{
					double totalWeight = 1;

					// adjacents
					double weight = .6065;

					// adjacent x
					if (x > 0)
					{
						pixel += weight * heightMap[ym, x - 1];
						totalWeight += weight;
					}
					if (x < numX - 1)
					{
						pixel += weight * heightMap[ym, x + 1];
						totalWeight += weight;
					}

					// adjacent y
					if (ym > 0)
					{
						pixel += weight * heightMap[(ym - 1), x];
						totalWeight += weight;
					}
					if (ym < numY - 1)
					{
						pixel += weight * heightMap[(ym + 1), x];
						totalWeight += weight;
					}

					// diagonal
					weight = .3679;

					if (x > 0)
					{
						if (ym > 0)
						{
							pixel += weight * heightMap[(ym - 1), x - 1];
							totalWeight += weight;
						}
						if (ym < numY - 1)
						{
							pixel += weight * heightMap[(ym + 1), x - 1];
							totalWeight += weight;
						}
					}

					if (x < numX - 1)
					{
						if (ym > 0)
						{
							pixel += weight * heightMap[(ym - 1), x + 1];
							totalWeight += weight;
						}
						if (ym < numY - 1)
						{
							pixel += weight * heightMap[(ym + 1), x + 1];
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
