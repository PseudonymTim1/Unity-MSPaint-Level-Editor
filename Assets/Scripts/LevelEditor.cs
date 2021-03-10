using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

// TODO: Cleanup!

// TODO: Add conveniant RGB value color clipboard copy...
// TODO: Auto map creation toggle, automatically creates map after new floor is added
// TODO: Auto recreate map when trying to enter playmode...
// TODO: Maybe add a position offset for mapobjects...

    ///////////////////////////////////////////////////////////////////////////////////////////////
   //                          Written By Pseudonym_Tim 2020                                    //
  // A simple but flexible level editor that reads pixel data from textures to construct maps  //
 ///////////////////////////////////////////////////////////////////////////////////////////////

/// <summary>
/// Simple easy to use level editor that
/// uses texture pixel data to place objects in world 
/// in the Unity editor or runtime
/// </summary>
public class LevelEditor : MonoBehaviour
{
    private const string LEVEL_EDITOR_ERROR_PREFIX = "LEVEL_EDITOR: ";
    private const string EDITOR_ROOT_PATH = "/LevelEditor/";
    private const string MAP_DIR_PATH = "/LevelEditor/Map";
    private const string FLOOR_PLAN_PATH = "/FloorPlans/";
    public const string FLOOR_PLAN_NAMEPREFIX = "FloorPlan";

    [Header("Editor Settings")]
    public bool requirePlayerSpawnPoint = true;
    public bool saveMapEditsToScene = true;
    public bool showPrevFloor = false;

    [Header("Map Settings")]
    public Transform mapParent;
    public Transform playerTransform;

    [Header("Maps")]
    public int selectedMapIndex; // What map are we editing/loading?
    public List<Map> maps = new List<Map>();
    private List<Transform> floorPlanParentTransforms = new List<Transform>();
    private List<Transform> mapParentChildren = new List<Transform>();

    private void Start()
    {
        // Make sure our map transform exists so we can parent stuff to it
        if(!mapParent)
        {
            Debug.LogWarning(LEVEL_EDITOR_ERROR_PREFIX + "Creating a map when not in play mode won't work if you don't have a parent set before runtime!");
            mapParent = new GameObject("Map").transform;
            mapParent.transform.position = Vector3.zero;
        }
        else
        {
            mapParent.transform.position = Vector3.zero; // Set it at the center of the world...
        }

        // Spawn checking...
        if(requirePlayerSpawnPoint)
        {
            if(CheckMultipleSpawnPoints()) { Debug.LogError(LEVEL_EDITOR_ERROR_PREFIX + "There cannot be more than one spawn point for each map, correct your floorplan duplicates and refresh!"); }

            // Complain if we didn't get a spawn point and we try to play the game...
            if(GetCurrentMap().playerSpawnPoint.spawnPos == Vector3.zero) { Debug.LogError(LEVEL_EDITOR_ERROR_PREFIX + "No player spawn point found or map wasn't refreshed!"); }

            // Spawn the player if we didn't get any issues...
            SpawnPlayer();
        }
    }

    public void ClearMap()
    {
        if(mapParentChildren.Count > 0)
        {
            for(int i = 0; i < mapParentChildren.Count; i++)
            {
                DestroyImmediate(mapParentChildren[i].gameObject, true);
            }

            mapParentChildren = new List<Transform>();

            foreach(FloorPlan floorPlan in GetCurrentMap().floorPlans)
            {
                // Make sure our objects list is cleared out as well...
                floorPlan.mapObjects = new List<GameObject>();
            }

            floorPlanParentTransforms = new List<Transform>();

            SaveChanges();
        }
    }

    public void SaveChanges()
    {
        if(saveMapEditsToScene)
        {
            // Save our scene...
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }

    public void RecreateFloorPlans()
    {
        Map map = GetCurrentMap();

        int floorIndex = 0;

        // Load texture...
        List<Texture2D> floorPlanTexturesInDirectory = GetFilesAtPath<Texture2D>(MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH).ToList();

        // Create the textures for each floor of this map...
        foreach(FloorPlan floorPlan in map.floorPlans)
        {
            // Setup and store the texture for this floorPlan...
            floorPlan.floorPlanTexture = SetupFloorCanvas(floorPlan);
            floorIndex++;

            // Save them...
            SaveFloorTextures(floorPlan.floorPlanTexture, floorIndex);

            // Assign our new texture...
            if(floorPlanTexturesInDirectory[floorIndex - 1])
            {
                floorPlan.floorPlanTexture = floorPlanTexturesInDirectory[floorIndex - 1];
            }
        }
    }

    public void RefreshFloorPlans()
    {
        // When the floor list count has been changed, we should make that new floor texture,
        // or delete the floor texture removed, and not recreate ALL of them

        Map map = GetCurrentMap();

        string floorPlanDirPath = Application.dataPath + MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH;
        byte[] texBytesNew = null;

        // Always enforce a single floor plan...
        if(map.floorPlans.Count <= 0)
        {
            FloorPlan floorPlan = new FloorPlan();

            floorPlan.floorPlanTexture = SetupFloorCanvas(floorPlan);

            map.floorPlans.Add(floorPlan);
        }

        if(map.floorPlans.Count > 0)
        {
            // Always update our first floor plan texture...
            if(File.Exists(floorPlanDirPath + FLOOR_PLAN_NAMEPREFIX + 1 + ".png"))
            {
                texBytesNew = File.ReadAllBytes(floorPlanDirPath + FLOOR_PLAN_NAMEPREFIX + 1 + ".png");
            }
            
            SetTextureImporterFormat(map.floorPlans[0].floorPlanTexture, true); // Make sure it's readable...

            map.floorPlans[0].floorPlanTexture.LoadImage(texBytesNew, false);
        }

        GetMapFloorChildren();

        int floorIndex = 0;
        int moddedFloorIndex = 0;

        List<Texture2D> floorPlanTextures = new List<Texture2D>();

        // Create the textures for each floor of this map if we add to our floorplan list...
        foreach(FloorPlan floorPlan in map.floorPlans)
        {
            floorIndex++;
            moddedFloorIndex++;

            // Setup, assign and save one...

            floorPlan.floorPlanTexture = SetupFloorCanvas(floorPlan);

            // Get the previous floor texture data and apply it to this floors for convieniance...

            // Make sure it exists first...

            if(showPrevFloor && (moddedFloorIndex - 1) >= 0) { moddedFloorIndex = moddedFloorIndex - 1; }

            if(showPrevFloor)
            {
                if(File.Exists(floorPlanDirPath + FLOOR_PLAN_NAMEPREFIX + (moddedFloorIndex + 1) + ".png"))
                {
                    texBytesNew = File.ReadAllBytes(floorPlanDirPath + FLOOR_PLAN_NAMEPREFIX + (moddedFloorIndex + 1) + ".png");

                    ApplyTextureChanges(floorPlan.floorPlanTexture, texBytesNew);
                }
            }
            else
            {
                if(File.Exists(floorPlanDirPath + FLOOR_PLAN_NAMEPREFIX + (moddedFloorIndex) + ".png"))
                {
                    texBytesNew = File.ReadAllBytes(floorPlanDirPath + FLOOR_PLAN_NAMEPREFIX + (moddedFloorIndex) + ".png");

                    ApplyTextureChanges(floorPlan.floorPlanTexture, texBytesNew);
                }
            }
            
            // Save the texture...
            SaveFloorTextures(floorPlan.floorPlanTexture, floorIndex);

            // Set the name and add it to the list
            floorPlan.floorPlanTexture.name = FLOOR_PLAN_NAMEPREFIX + floorIndex;
            floorPlanTextures.Add(floorPlan.floorPlanTexture);
        }

        // If they were removed from the list, delete the floor plan texture...

        // Search through our floor plan texture directory, grab all floorplan textures and put them in a list
        List<Texture2D> floorPlanTexturesInDirectory = GetFilesAtPath<Texture2D>(MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH).ToList();

        // Store the difference between the lists, these are the textures we want to make go bye bye
        List<Texture2D> texturesToDelete = floorPlanTexturesInDirectory.Where(item => !floorPlanTextures.Any(item2 => item2.name == item.name)).ToList();

        // Delete them...
        foreach(Texture2D tex in texturesToDelete)
        {
            File.Delete(Application.dataPath + MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH + tex.name + ".png");
        }

        // Refresh our asset database...
        RefreshAssetDatabase();
    }

    private void ApplyTextureChanges(Texture2D floorPlanTexture, byte[] newBytes)
    {
        floorPlanTexture.LoadImage(newBytes);

        floorPlanTexture.Apply();
    }

    public void CreateEditorDirectory()
    {
        string floorPlanDirPath = Application.dataPath + MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH;

        if(!Directory.Exists(floorPlanDirPath))
        {
            Directory.CreateDirectory(floorPlanDirPath);

            Map map = GetCurrentMap();

            map.floorPlans = new List<FloorPlan>();

            // Create our first empty floor plan here automatically and assign it...
            FloorPlan floorPlan = new FloorPlan();

            floorPlan.floorPlanTexture = SetupFloorCanvas(floorPlan);

            map.floorPlans.Add(floorPlan);

            SaveFloorTextures(floorPlan.floorPlanTexture, 1);

            RefreshFloorPlans();

            RefreshAssetDatabase();
        }
    }

    // Cool little piece of code to change a texture asset to be readable without doing it manually in the editor...
    public static void SetTextureImporterFormat(Texture2D texture, bool isReadable)
    {
        if(!texture) return;

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture);
        UnityEditor.TextureImporter texImporter = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;

        if(texImporter != null)
        {
            texImporter.textureType = UnityEditor.TextureImporterType.Default;

            texImporter.isReadable = isReadable;

            UnityEditor.AssetDatabase.ImportAsset(assetPath);
            UnityEditor.AssetDatabase.Refresh();
        }
    }

    private void OnValidate()
    {
        // (Make sure we refresh the list and our changes in the directory every time we change a value)...
        RefreshFloorPlans();
    }

    private void GetMapFloorChildren()
    {
        mapParentChildren = new List<Transform>();

        foreach(Transform child in mapParent.transform)
        {
            mapParentChildren.Add(child.transform);
        }
    }

    public static T[] GetFilesAtPath<T>(string path)
    {
        ArrayList al = new ArrayList();
        string[] fileEntries = Directory.GetFiles(Application.dataPath + "/" + path);

        foreach(string fileName in fileEntries)
        {
            int index = fileName.LastIndexOf("/");
            string localPath = "Assets/" + path;

            if(index > 0) { localPath += fileName.Substring(index); }

            Object t = UnityEditor.AssetDatabase.LoadAssetAtPath(localPath, typeof(T));

            if(t != null) { al.Add(t); }
        }

        T[] result = new T[al.Count];

        for(int i = 0; i < al.Count; i++) { result[i] = (T)al[i]; }

        return result;
    }

    private void SetFloorColorMap(FloorPlan floorPlan)
    {
        floorPlan.colorMap = new Color[floorPlan.mapSize, floorPlan.mapSize];

        for(int x = 0; x < floorPlan.mapSize; x++)
        {
            for(int z = 0; z < floorPlan.mapSize; z++)
            {
                floorPlan.colorMap[x, z] = floorPlan.floorPlanTexture.GetPixel(x, z);
            }
        }
    }

    private Texture2D SetupFloorCanvas(FloorPlan floorPlan)
    {
        Texture2D floorTexture = new Texture2D(floorPlan.mapSize, floorPlan.mapSize, TextureFormat.RGBA32, false);

        // Set it to solid white...
        Color fillColor = Color.white;
        var fillColorArray = floorTexture.GetPixels();

        for(var i = 0; i < fillColorArray.Length; ++i)
        {
            fillColorArray[i] = fillColor;
        }

        floorTexture.SetPixels(fillColorArray);

        SetTextureImporterFormat(floorTexture, true); // Make sure it's readable...

        return floorTexture;
    }

    private void SaveFloorTextures(Texture2D texture, int floorIndex)
    {
        // Set a black dot in the middle so we know where the center of the map is...
        texture.SetPixel(texture.width / 2, texture.height / 2, Color.black); 
        texture.Apply();

        byte[] texBytes = texture.EncodeToPNG();
        
        string floorDirPath = Application.dataPath + MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH;

        if(!Directory.Exists(floorDirPath)) { Directory.CreateDirectory(floorDirPath); }

        // Save floor texture as PNG
        File.WriteAllBytes(floorDirPath + FLOOR_PLAN_NAMEPREFIX + floorIndex + ".png", texBytes);

        RefreshAssetDatabase(); // Refresh our database now to reflect our changes...
    }

    public void DeleteLevelEditorDirectory()
    {
        string floorDirPath = Application.dataPath + MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH;

        if(Directory.Exists(Application.dataPath + EDITOR_ROOT_PATH))
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Application.dataPath + EDITOR_ROOT_PATH);
            DirectoryInfo dirInfoFloorPath = new DirectoryInfo(Application.dataPath + MAP_DIR_PATH + selectedMapIndex + FLOOR_PLAN_PATH);

            // Piss off
            foreach(FileInfo file in dirInfoFloorPath.GetFiles()) { file.Delete(); }
            foreach(FileInfo file in dirInfo.GetFiles()) { file.Delete(); }
            foreach(DirectoryInfo dir in dirInfo.GetDirectories()) { dir.Delete(true); }

            Directory.Delete(Application.dataPath + EDITOR_ROOT_PATH);

            RefreshAssetDatabase(); // Refresh our database now to reflect our changes...
        }
    }

    private void CreateTestDungeon()
    {

    }

    private bool CheckMultipleSpawnPoints()
    {
        int playerSpawnAmount = 0;

        foreach(FloorPlan floorPlan in GetCurrentMap().floorPlans)
        {
            SetFloorColorMap(floorPlan); // Set the floorPlan color map...

            for(int x = 0; x < floorPlan.mapSize; x++)
            {
                for(int z = 0; z < floorPlan.mapSize; z++)
                {
                    // Position...
                    Vector2Int pos = new Vector2Int(x, z);

                    // This pixel color...
                    Color pixelColor = floorPlan.colorMap[pos.x, pos.y];

                    // We got a spawn point?
                    bool playerSpawnpointExists = pixelColor == GetCurrentMap().playerSpawnPoint.pixelColor; 

                    // Complain about multiple spawn points for one map...
                    if(playerSpawnpointExists) { playerSpawnAmount++; if(playerSpawnAmount > 1) { return true; } }
                }
            }
        }

        return false;
    }

    private void SpawnPlayer()
    {
        foreach(FloorPlan floorPlan in GetCurrentMap().floorPlans)
        {
            SetFloorColorMap(floorPlan); // Set the floorPlan color map...

            for(int x = 0; x < floorPlan.mapSize; x++)
            {
                for(int z = 0; z < floorPlan.mapSize; z++)
                {
                    // Position...
                    Vector2Int pos = new Vector2Int(x, z);

                    // This pixel color...
                    Color pixelColor = floorPlan.colorMap[pos.x, pos.y];

                    // We got a spawn point?
                    bool playerSpawnpointExists = pixelColor == GetCurrentMap().playerSpawnPoint.pixelColor;

                    if(playerSpawnpointExists) { SpawnPlayer(GetCurrentMap(), pos, floorPlan); } // Spawn the player here...
                }
            }
        }
    }

    private void RefreshAssetDatabase()
    {
        #if UNITY_EDITOR

        // Make sure we're not in play mode or switching to it!
        if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) { UnityEditor.AssetDatabase.Refresh(); }
        
        #endif
    }

    /// <summary>
    // Reads the pixel data from all the floor textures and 
    // constructs the appropriate map objects in the world 
    /// </summary>
    public void CreateMap()
    {
        GetMapFloorChildren(); // REMOVE???

        ClearMap(); // Make sure we get rid of our map objects...

        Map map = GetCurrentMap();

        int floorPlanIndex = 0;

        Transform floorPlanTransform = null;
        floorPlanParentTransforms = new List<Transform>();

        // Set map objects for each floorPlan....
        foreach(FloorPlan floorPlan in map.floorPlans)
        {
            // Set floor plan parents...
            floorPlanTransform = new GameObject(FLOOR_PLAN_NAMEPREFIX + floorPlanIndex).transform;
            floorPlanParentTransforms.Add(floorPlanTransform);
            floorPlanTransform.transform.SetParent(mapParent);

            // Make sure the list is cleared out...
            floorPlan.mapObjects.Clear();

            floorPlanIndex++;

            Color32[] pixels = floorPlan.floorPlanTexture.GetPixels32();

            SetFloorColorMap(floorPlan); // Set the floorPlan color map...

            // Construct the map, converting pixels to map objects and placing them in the world...
            for(int x = 0; x < floorPlan.mapSize; x++)
            {
                for(int z = 0; z < floorPlan.mapSize; z++)
                {
                    // Position...
                    Vector2Int pos = new Vector2Int(x, z);

                    // This pixel color...
                    Color pixelColor = floorPlan.colorMap[pos.x, pos.y];

                    // Does the color of this pixel match a map object's?
                    bool colorExists = map.mapObjects.Where(obj => obj.pixelColor == pixelColor).Any();

                    // Make map object for this color...
                    if(colorExists)
                    {
                        // Get the map object that the matches the color
                        GameObject mapObj = map.mapObjects.Where(obj => obj.pixelColor == pixelColor).SingleOrDefault().mapObject;

                        // Create the map object at this position in the world and set it's parent...
                        GameObject instantiatedObj = Instantiate(mapObj, new Vector3(pos.x, floorPlan.groundHeight, pos.y), Quaternion.identity);
                        instantiatedObj.transform.SetParent(floorPlanTransform);

                        floorPlan.mapObjects.Add(instantiatedObj);
                    }
                }
            }

            floorPlan.floorPlanCreated = true;

            floorPlanTransform.transform.position = new Vector3(0, floorPlanIndex - 1, 0);
        }

        // Create the floor and ceiling...
        CreateCeiling(map, map.floorPlans.Last(), floorPlanParentTransforms.Last());
        CreateGround(map, map.floorPlans.First(), floorPlanParentTransforms.First());

        GetMapFloorChildren();

        SaveChanges(); // Save our scene...

        // Spawn the player
        if(requirePlayerSpawnPoint && !Application.isPlaying) { SpawnPlayer(); }
    }

    /// <summary>
    /// Override this to apply your own spawn behaviour for player...
    /// </summary>
    public virtual void SpawnPlayer(Map map, Vector2Int pos, FloorPlan floorPlan)
    {
        map.playerSpawnPoint.spawnPos = new Vector3(pos.x, floorPlan.groundHeight, pos.y);

        // Set player position and rotation to reflect our player spawn point...
        playerTransform.transform.position = new Vector3(pos.x, 0, pos.y);
        playerTransform.transform.eulerAngles = new Vector3(0, map.playerSpawnPoint.faceAngle, 0);

        // Correct our scene cam...
        CorrectSceneCam(new Vector3(pos.x, 0, pos.y), playerTransform.forward);
    }

    /// <summary>
    /// Sort of a HACK, "Lerp" our scene camera position and look direction
    /// </summary>
    public static void CorrectSceneCam(Vector3 camPos, Vector3 camForward)
    {
        //UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Scene"); // Switch to scene view...

        UnityEditor.SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

        // Can't set position directly as it's updated every frame, we can align it though...
        if(sceneView != null)
        {
            sceneView.orthographic = false;

            Transform target = sceneView.camera.transform;
            target.transform.position = camPos;
            target.transform.rotation = Quaternion.LookRotation(camForward);
            sceneView.AlignViewToObject(target.transform);
        }
    }

    private void CreateCeiling(Map map, FloorPlan floorPlan, Transform floorParent)
    {
        if(!map.generateCeiling) { return; }

        MeshRenderer ceilingRenderer = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
        ceilingRenderer.gameObject.name = "Ceiling";

        ceilingRenderer.material = map.ceilingMaterial;

        ceilingRenderer.transform.localScale = new Vector3(floorPlan.mapSize, floorPlan.mapSize, floorPlan.mapSize);

        ceilingRenderer.transform.position = new Vector3(floorPlan.mapSize / 2, floorParent.transform.position.y + floorPlan.ceilingHeight, floorPlan.mapSize / 2);
        ceilingRenderer.transform.eulerAngles = new Vector3(270, 0, 0);

        floorPlan.mapObjects.Add(ceilingRenderer.gameObject);
        ceilingRenderer.transform.SetParent(floorParent);
    }

    private void CreateGround(Map map, FloorPlan floorPlan, Transform floorParent)
    {
        if(!map.generateGround) { return; }

        MeshRenderer floorRenderer = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
        floorRenderer.gameObject.name = "Ground";

        floorRenderer.material = map.groundMaterial;

        floorRenderer.transform.localScale = new Vector3(floorPlan.mapSize, floorPlan.mapSize, floorPlan.mapSize);
        floorRenderer.transform.position = new Vector3(floorPlan.mapSize / 2, floorPlan.groundHeight, floorPlan.mapSize / 2);

        floorRenderer.transform.eulerAngles = new Vector3(90, 0, 0);

        floorPlan.mapObjects.Add(floorRenderer.gameObject);
        floorRenderer.transform.SetParent(floorParent);
    }

    private Map GetCurrentMap() { return maps[selectedMapIndex]; }
}

[System.Serializable]
public class Map
{
    [HideInInspector] public string mapName = "NewMap";
    public PlayerSpawnPoint playerSpawnPoint;
    public List<MapObject> mapObjects = new List<MapObject>();
    public List<FloorPlan> floorPlans = new List<FloorPlan>();
    public Material groundMaterial;
    public Material ceilingMaterial;
    public bool generateCeiling = true;
    public bool generateGround = true;
}

[System.Serializable]
public class FloorPlan
{
    [HideInInspector] public string floorPlanName = LevelEditor.FLOOR_PLAN_NAMEPREFIX;
    public int mapSize = 64;
    [HideInInspector] public float ceilingHeight = 0.5f;
    [HideInInspector] public float groundHeight = -0.5f;
    [HideInInspector] public Texture2D floorPlanTexture;
    public Color[,] colorMap;
    [HideInInspector] public List<GameObject> mapObjects = new List<GameObject>();
    [HideInInspector] public bool floorPlanCreated = false;
}

[System.Serializable]
public class PlayerSpawnPoint
{
    [HideInInspector] public Vector3 spawnPos;
    public float faceAngle;
    public Color pixelColor = Color.green;
}

[System.Serializable]
public class MapObject
{
    public string objectName = "New Object";
    public Color pixelColor = Color.black;
    public GameObject mapObject;
}