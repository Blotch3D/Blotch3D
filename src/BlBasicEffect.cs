#region File Description
//-----------------------------------------------------------------------------
// BasicEffectWithAlphaTest is adapted from BasicEffect
//
// BasicEffect is part of
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Blotch
{
	/// <summary>
	/// Holds a Blotch3D custom effect (like BlBasicEffectAlphaTest and BlBasicEffectClipColor) that is exactly
	/// like MonoGame's BasicEffect but with certain added features.
	/// To make a BlBasicEffect, you must pass a byte array to the constructor containing the shader code loaded
	/// from the shader file (mgfxo file extension). See the SpriteAlphaTexture example and the section on Translucency
	/// for details on how to make
	/// and use objects of this class.
	/// </summary>
	public class BlBasicEffect : Effect, IEffectMatrices, IEffectLights, IEffectFog
	{
		#region Effect Parameters

		EffectParameter textureParam;
		EffectParameter diffuseColorParam;
		EffectParameter emissiveColorParam;
		EffectParameter specularColorParam;
		EffectParameter specularPowerParam;
		EffectParameter eyePositionParam;
		EffectParameter fogColorParam;
		EffectParameter fogVectorParam;
		EffectParameter worldParam;
		EffectParameter worldInverseTransposeParam;
		EffectParameter worldViewProjParam;

		#endregion

		#region Fields

		bool lightingEnabled;
		bool preferPerPixelLighting;
		bool oneLight;
		bool fogEnabled;
		bool textureEnabled;
		bool vertexColorEnabled;

		Matrix world = Matrix.Identity;
		Matrix view = Matrix.Identity;
		Matrix projection = Matrix.Identity;

		Matrix worldView;

		Vector3 diffuseColor = Vector3.One;
		Vector3 emissiveColor = Vector3.Zero;
		Vector3 ambientLightColor = Vector3.Zero;

		float alpha = 1;

		DirectionalLight light0;
		DirectionalLight light1;
		DirectionalLight light2;

		float fogStart = 0;
		float fogEnd = 1;

		BlEffectDirtyFlags dirtyFlags = BlEffectDirtyFlags.All;

		#endregion

		#region Public Properties


		/// <summary>
		/// Gets or sets the world matrix.
		/// </summary>
		public Matrix World
		{
			get { return world; }

			set
			{
				world = value;
				dirtyFlags |= BlEffectDirtyFlags.World | BlEffectDirtyFlags.WorldViewProj | BlEffectDirtyFlags.Fog;
			}
		}


		/// <summary>
		/// Gets or sets the view matrix.
		/// </summary>
		public Matrix View
		{
			get { return view; }

			set
			{
				view = value;
				dirtyFlags |= BlEffectDirtyFlags.WorldViewProj | BlEffectDirtyFlags.EyePosition | BlEffectDirtyFlags.Fog;
			}
		}


		/// <summary>
		/// Gets or sets the projection matrix.
		/// </summary>
		public Matrix Projection
		{
			get { return projection; }

			set
			{
				projection = value;
				dirtyFlags |= BlEffectDirtyFlags.WorldViewProj;
			}
		}


		/// <summary>
		/// Gets or sets the material diffuse color (range 0 to 1).
		/// </summary>
		public Vector3 DiffuseColor
		{
			get { return diffuseColor; }

			set
			{
				diffuseColor = value;
				dirtyFlags |= BlEffectDirtyFlags.MaterialColor;
			}
		}


		/// <summary>
		/// Gets or sets the material emissive color (range 0 to 1).
		/// </summary>
		public Vector3 EmissiveColor
		{
			get { return emissiveColor; }

			set
			{
				emissiveColor = value;
				dirtyFlags |= BlEffectDirtyFlags.MaterialColor;
			}
		}


		/// <summary>
		/// Gets or sets the material specular color (range 0 to 1).
		/// </summary>
		public Vector3 SpecularColor
		{
			get { return specularColorParam.GetValueVector3(); }
			set { specularColorParam.SetValue(value); }
		}


		/// <summary>
		/// Gets or sets the material specular power.
		/// </summary>
		public float SpecularPower
		{
			get { return specularPowerParam.GetValueSingle(); }
			set { specularPowerParam.SetValue(value); }
		}


		/// <summary>
		/// Gets or sets the material alpha.
		/// </summary>
		public float Alpha
		{
			get { return alpha; }

			set
			{
				alpha = value;
				dirtyFlags |= BlEffectDirtyFlags.MaterialColor;
			}
		}

		/// <inheritdoc/>
		public bool LightingEnabled
		{
			get { return lightingEnabled; }

			set
			{
				if (lightingEnabled != value)
				{
					lightingEnabled = value;
					dirtyFlags |= BlEffectDirtyFlags.ShaderIndex | BlEffectDirtyFlags.MaterialColor;
				}
			}
		}


		/// <summary>
		/// Gets or sets the per-pixel lighting prefer flag.
		/// </summary>
		public bool PreferPerPixelLighting
		{
			get { return preferPerPixelLighting; }

			set
			{
				if (preferPerPixelLighting != value)
				{
					preferPerPixelLighting = value;
					dirtyFlags |= BlEffectDirtyFlags.ShaderIndex;
				}
			}
		}


		/// <inheritdoc/>
		public Vector3 AmbientLightColor
		{
			get { return ambientLightColor; }

			set
			{
				ambientLightColor = value;
				dirtyFlags |= BlEffectDirtyFlags.MaterialColor;
			}
		}


		/// <inheritdoc/>
		public DirectionalLight DirectionalLight0 { get { return light0; } }


		/// <inheritdoc/>
		public DirectionalLight DirectionalLight1 { get { return light1; } }


		/// <inheritdoc/>
		public DirectionalLight DirectionalLight2 { get { return light2; } }


		/// <inheritdoc/>
		public bool FogEnabled
		{
			get { return fogEnabled; }

			set
			{
				if (fogEnabled != value)
				{
					fogEnabled = value;
					dirtyFlags |= BlEffectDirtyFlags.ShaderIndex | BlEffectDirtyFlags.FogEnable;
				}
			}
		}


		/// <inheritdoc/>
		public float FogStart
		{
			get { return fogStart; }

			set
			{
				fogStart = value;
				dirtyFlags |= BlEffectDirtyFlags.Fog;
			}
		}


		/// <inheritdoc/>
		public float FogEnd
		{
			get { return fogEnd; }

			set
			{
				fogEnd = value;
				dirtyFlags |= BlEffectDirtyFlags.Fog;
			}
		}


		/// <inheritdoc/>
		public Vector3 FogColor
		{
			get { return fogColorParam.GetValueVector3(); }
			set { fogColorParam.SetValue(value); }
		}


		/// <summary>
		/// Gets or sets whether texturing is enabled.
		/// </summary>
		public bool TextureEnabled
		{
			get { return textureEnabled; }

			set
			{
				if (textureEnabled != value)
				{
					textureEnabled = value;
					dirtyFlags |= BlEffectDirtyFlags.ShaderIndex;
				}
			}
		}


		/// <summary>
		/// Gets or sets the current texture.
		/// </summary>
		public Texture2D Texture
		{
			get { return textureParam.GetValueTexture2D(); }
			set { textureParam.SetValue(value); }
		}


		/// <summary>
		/// Gets or sets whether vertex color is enabled.
		/// </summary>
		public bool VertexColorEnabled
		{
			get { return vertexColorEnabled; }

			set
			{
				if (vertexColorEnabled != value)
				{
					vertexColorEnabled = value;
					dirtyFlags |= BlEffectDirtyFlags.ShaderIndex;
				}
			}
		}


		#endregion

		#region Methods

		/// <summary>
		/// Creates a new BlBasicEffect with default parameter settings. See class description for more info.
		/// </summary>
		public BlBasicEffect(GraphicsDevice device, byte[] bytes)
			: base(device, bytes)
		{
			CacheEffectParameters(null);

			DirectionalLight0.Enabled = true;
			SpecularColor = Vector3.One;
			SpecularPower = 16;
		}

		/// <summary>
		/// Creates a new BlBasicEffect by cloning parameter settings from an existing instance.
		/// </summary>
		protected BlBasicEffect(BlBasicEffect cloneSource)
			: base(cloneSource)
		{
			CacheEffectParameters(cloneSource);

			lightingEnabled = cloneSource.lightingEnabled;
			preferPerPixelLighting = cloneSource.preferPerPixelLighting;
			fogEnabled = cloneSource.fogEnabled;
			textureEnabled = cloneSource.textureEnabled;
			vertexColorEnabled = cloneSource.vertexColorEnabled;

			world = cloneSource.world;
			view = cloneSource.view;
			projection = cloneSource.projection;

			diffuseColor = cloneSource.diffuseColor;
			emissiveColor = cloneSource.emissiveColor;
			ambientLightColor = cloneSource.ambientLightColor;

			alpha = cloneSource.alpha;

			fogStart = cloneSource.fogStart;
			fogEnd = cloneSource.fogEnd;
		}


		/// <summary>
		/// Creates a clone of the current BasicEffectWithAlphaTest instance.
		/// </summary>
		public override Effect Clone()
		{
			return new BlBasicEffect(this);
		}


		/// <inheritdoc/>
		public void EnableDefaultLighting()
		{
			LightingEnabled = true;

			AmbientLightColor = BlEffectHelpers.EnableDefaultLighting(light0, light1, light2);
		}


		/// <summary>
		/// Looks up shortcut references to our effect parameters.
		/// </summary>
		void CacheEffectParameters(BlBasicEffect cloneSource)
		{
			textureParam = Parameters["Texture"];
			diffuseColorParam = Parameters["DiffuseColor"];
			emissiveColorParam = Parameters["EmissiveColor"];
			specularColorParam = Parameters["SpecularColor"];
			specularPowerParam = Parameters["SpecularPower"];
			eyePositionParam = Parameters["EyePosition"];
			fogColorParam = Parameters["FogColor"];
			fogVectorParam = Parameters["FogVector"];
			worldParam = Parameters["World"];
			worldInverseTransposeParam = Parameters["WorldInverseTranspose"];
			worldViewProjParam = Parameters["WorldViewProj"];

			light0 = new DirectionalLight(Parameters["DirLight0Direction"],
										  Parameters["DirLight0DiffuseColor"],
										  Parameters["DirLight0SpecularColor"],
										  (cloneSource != null) ? cloneSource.light0 : null);

			light1 = new DirectionalLight(Parameters["DirLight1Direction"],
										  Parameters["DirLight1DiffuseColor"],
										  Parameters["DirLight1SpecularColor"],
										  (cloneSource != null) ? cloneSource.light1 : null);

			light2 = new DirectionalLight(Parameters["DirLight2Direction"],
										  Parameters["DirLight2DiffuseColor"],
										  Parameters["DirLight2SpecularColor"],
										  (cloneSource != null) ? cloneSource.light2 : null);
		}


		/// <summary>
		/// Lazily computes derived parameter values immediately before applying the effect.
		/// </summary>
		protected override void OnApply()
		{
			// Recompute the world+view+projection matrix or fog vector?
			dirtyFlags = BlEffectHelpers.SetWorldViewProjAndFog(dirtyFlags, ref world, ref view, ref projection, ref worldView, fogEnabled, fogStart, fogEnd, worldViewProjParam, fogVectorParam);

			// Recompute the diffuse/emissive/alpha material color parameters?
			if ((dirtyFlags & BlEffectDirtyFlags.MaterialColor) != 0)
			{
				BlEffectHelpers.SetMaterialColor(lightingEnabled, alpha, ref diffuseColor, ref emissiveColor, ref ambientLightColor, diffuseColorParam, emissiveColorParam);

				dirtyFlags &= ~BlEffectDirtyFlags.MaterialColor;
			}

			if (lightingEnabled)
			{
				// Recompute the world inverse transpose and eye position?
				dirtyFlags = BlEffectHelpers.SetLightingMatrices(dirtyFlags, ref world, ref view, worldParam, worldInverseTransposeParam, eyePositionParam);


				// Check if we can use the only-bother-with-the-first-light shader optimization.
				bool newOneLight = !light1.Enabled && !light2.Enabled;

				if (oneLight != newOneLight)
				{
					oneLight = newOneLight;
					dirtyFlags |= BlEffectDirtyFlags.ShaderIndex;
				}
			}

			// Recompute the shader index?
			if ((dirtyFlags & BlEffectDirtyFlags.ShaderIndex) != 0)
			{
				int shaderIndex = 0;

				if (!fogEnabled)
					shaderIndex += 1;

				if (vertexColorEnabled)
					shaderIndex += 2;

				if (textureEnabled)
					shaderIndex += 4;

				if (lightingEnabled)
				{
					if (preferPerPixelLighting)
						shaderIndex += 24;
					else if (oneLight)
						shaderIndex += 16;
					else
						shaderIndex += 8;
				}

				dirtyFlags &= ~BlEffectDirtyFlags.ShaderIndex;

				CurrentTechnique = Techniques[shaderIndex];
			}
		}


		#endregion
	}
}

