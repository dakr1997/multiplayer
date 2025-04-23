using UnityEngine;
using TMPro;
public class FloatingText : MonoBehaviour
{
    public event System.Action OnTextComplete;
    public float moveSpeed = 50f;
    public float fadeDuration = 1f;

    private TextMeshProUGUI text;
    private float startTime;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        startTime = Time.time;
    }

    void Update()
    {
        // Move upward
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // Fade out
        float alpha = Mathf.Lerp(1f, 0f, (Time.time - startTime) / fadeDuration);
        if (text != null)
        {
            var color = text.color;
            color.a = alpha;
            text.color = color;
        }

        if (alpha <= 0f)
            Destroy(gameObject);
    }

    public void SetText(string content)
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();
        text.text = content;
    }

    public void Complete()
    {
        OnTextComplete?.Invoke();
    }
}
