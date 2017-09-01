using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AddBrickToPanel : MonoBehaviour {

    public GameObject brickPanel;
    public Object brickButtonPrefab;
    public PlacerBrick placerBrickScript;
    const float distance_from_border = 10.0f;
    const float distance_between_bricks = 3.0f;
    const int bricks_per_row = 8;
    const int brick_size = 64;
    int number_of_brick_types = 0;

    public void AddBlockToPanel(string blockName, string filename)
    {
        Object resource = Resources.Load("Bricks/Icons/" + filename + "-icon");
        Texture2D textureForButton = resource as Texture2D;
        string buttonName = "btn" + filename;
        Button button = (Instantiate(brickButtonPrefab) as GameObject).GetComponent<Button>();
        button.gameObject.transform.SetParent(brickPanel.transform);
        button.name = buttonName;
        Image buttonImage = button.transform.gameObject.GetComponent<Image>();
        buttonImage.sprite = Sprite.Create(textureForButton, new Rect(0, 0, textureForButton.width,
                                                                            textureForButton.height),
                                                                            new Vector2(0.0f, 0.0f));

        float x = distance_from_border + (number_of_brick_types % bricks_per_row) * (distance_between_bricks + brick_size);
        float y = -(distance_from_border + ((int)(number_of_brick_types / bricks_per_row)) * (distance_between_bricks + brick_size));
        RectTransform rectTransform = button.gameObject.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.0f, 1.0f);
        rectTransform.anchoredPosition = new Vector3(x, y, 0.0f);
        rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

        button.onClick.AddListener(() => 
        {
            brickPanel.GetComponent<PanelUI>().toggleBrickPanel();
            placerBrickScript.CreatePlacer(filename);
        });

        number_of_brick_types++;
    }

	// Use this for initialization
	void Start () {
        placerBrickScript = GameObject.Find("PlacerBrickScript").GetComponent<PlacerBrick>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
