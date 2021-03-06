﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public abstract class AssetLoadOperation : IEnumerator
{
    public object Current
    {
        get
        {
            return null;
        }
    }
    public bool MoveNext()
    {
        return !IsDone();
    }

    public void Reset()
    {
    }

    public virtual float Progress
    {
        get
        {
            return 0.0f;
        }
    }

    abstract public bool Update();

    abstract public bool IsDone();

    abstract public void Unload();
}

public abstract class AssetBundleLoadAssetOperation : AssetLoadOperation
{
    public abstract T GetAsset<T>() where T : UnityEngine.Object;
}

public class AssetBundleLoadAssetSimulationOperation : AssetBundleLoadAssetOperation
{
    Object simulatedObject;

    public AssetBundleLoadAssetSimulationOperation(Object simulatedObject)
    {
        this.simulatedObject = simulatedObject;
    }

    public override T GetAsset<T>()
    {
        return simulatedObject as T;
    }

    public override bool Update()
    {
        return false;
    }

    public override bool IsDone()
    {
        return true;
    }

    public override void Unload()
    {
    }
}

public class AssetBundleLoadAssetFullOperation : AssetBundleLoadAssetOperation
{
    protected string m_AssetBundleName;
    protected string m_AssetName;
    protected string m_DownloadingError;
    protected System.Type m_Type;
    protected AssetBundleRequest m_Request = null;

    public AssetBundleLoadAssetFullOperation(string bundleName, string assetName, System.Type type)
    {
        m_AssetBundleName = bundleName;
        m_AssetName = assetName;
        m_Type = type;
    }

    public override T GetAsset<T>()
    {
        if (m_Request != null && m_Request.isDone)
            return m_Request.asset as T;
        else
            return null;
    }

    // Returns true if more Update calls are required.
    public override bool Update()
    {
        if (m_Request != null)
            return false;

        LoadedAssetBundle bundle = AssetLoadManager.GetLoadedAssetBundle(m_AssetBundleName, out m_DownloadingError);
        if (bundle != null)
        {
            ///@TODO: When asset bundle download fails this throws an exception...
            m_Request = bundle.assetBundle.LoadAssetAsync(m_AssetName, m_Type);
            return false;
        }
        else
        {
            return true;
        }
    }

    public override float Progress
    {
        get
        {
            if (m_Request == null)
                return 0.0f;
            return m_Request.progress;
        }
    }

    public override bool IsDone()
    {
        // Return if meeting downloading error.
        // m_DownloadingError might come from the dependency downloading.
        if (m_Request == null && m_DownloadingError != null)
        {
            DebugLogger.LogErrorFormat("[AssetLoadManager]:{0}",m_DownloadingError);
            return true;
        }

        return m_Request != null && m_Request.isDone;
    }

    public override void Unload()
    {
        AssetLoadManager.UnLoadAssetBundle(m_AssetBundleName);
    }
}

#if UNITY_EDITOR
public class AssetBundleLoadLevelSimulationOperation : AssetLoadOperation
{
	AsyncOperation m_Operation = null;


	public AssetBundleLoadLevelSimulationOperation(string assetBundleName, string levelName, bool isAdditive)
	{
		string[] levelPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, levelName);
		if (levelPaths.Length == 0)
		{
			///@TODO: The error needs to differentiate that an asset bundle name doesn't exist
			//        from that there right scene does not exist in the asset bundle...

            DebugLogger.LogErrorFormat("[AssetLoadManager]:There is no scene with name : {0} in {1}" ,levelName, assetBundleName);
			return;
		}

		if (isAdditive)
			m_Operation = UnityEditor.EditorApplication.LoadLevelAdditiveAsyncInPlayMode(levelPaths[0]);
		else
			m_Operation = UnityEditor.EditorApplication.LoadLevelAsyncInPlayMode(levelPaths[0]);
	}

	public override bool Update()
	{
		return false;
	}

	public override bool IsDone()
	{
		return m_Operation == null || m_Operation.isDone;
	}

    public override void Unload()
    {
    }
}
#endif

public class AssetBundleLoadLevelOperation : AssetLoadOperation
{
	protected string m_AssetBundleName;
	protected string m_LevelName;
	protected bool m_IsAdditive;
	protected string m_DownloadingError;
	protected AsyncOperation m_Request;

	public AssetBundleLoadLevelOperation(string assetbundleName, string levelName, bool isAdditive)
	{
		m_AssetBundleName = assetbundleName;
		m_LevelName = levelName;
		m_IsAdditive = isAdditive;
	}

	public override bool Update()
	{
		if (m_Request != null)
			return false;

		LoadedAssetBundle bundle = AssetLoadManager.GetLoadedAssetBundle(m_AssetBundleName, out m_DownloadingError);
		if (bundle != null)
		{
            if (m_IsAdditive)
                m_Request = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(m_LevelName, LoadSceneMode.Additive);
			else
                m_Request = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(m_LevelName);
			return false;
		}
		else
			return true;
	}

	public override bool IsDone()
	{
		// Return if meeting downloading error.
		// m_DownloadingError might come from the dependency downloading.
		if (m_Request == null && m_DownloadingError != null)
		{
            DebugLogger.LogErrorFormat("[AssetLoadManager]:{0}",m_DownloadingError);
			return true;
		}

		return m_Request != null && m_Request.isDone;
	}

    public override void Unload()
    {
        AssetLoadManager.UnLoadAssetBundle(m_AssetBundleName);
    }

    public override float Progress
    {
        get
        {
            if (m_Request == null)
                return 0.0f;
            return m_Request.progress;
        }
    }
}



