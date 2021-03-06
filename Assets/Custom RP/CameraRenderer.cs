﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
	ScriptableRenderContext context;

	Camera camera;

	const string bufferName = "Render Camera";

	CullingResults cullingResults;

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

	static Material errorMaterial;

	public void Render(ScriptableRenderContext context, Camera camera)
	{
		this.context = context;
		this.camera = camera;

		if (!Cull())
			return;

		Setup();
		DrawVisibleGeometry();
		DrawUnsupportedShaders();
		Submit();
	}

	void DrawVisibleGeometry()
	{
		var sortingSettings = new SortingSettings(camera)
		{
			criteria = SortingCriteria.CommonOpaque
		};

		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		);

		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);


		context.DrawSkybox(camera);

		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	void DrawUnsupportedShaders()
	{
		if (errorMaterial == null)
		{
			errorMaterial =
				new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		)
		{
			overrideMaterial = errorMaterial
		};
		var filteringSettings = FilteringSettings.defaultValue;

		for (int i = 1; i < legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		
	}

	void Submit()
	{
		buffer.EndSample(bufferName);
		ExecuteBuffer();
		context.Submit();
	}

	void Setup()
	{
		context.SetupCameraProperties(camera);

		buffer.ClearRenderTarget(true, true, Color.clear);

		buffer.BeginSample(bufferName);

		ExecuteBuffer();
	}
	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	bool Cull()
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
			cullingResults = context.Cull(ref p);
			return true;
		}

		return false;
	}
}
