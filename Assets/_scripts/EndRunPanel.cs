using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EndRunPanel : MonoBehaviour
{
    [Header("Root del panel")]
    public GameObject root;

    [Header("Único texto del resumen (multilínea)")]
    public TextMeshProUGUI resumenBodyText;

    private bool shown;

    void Start()
    {
        if (root) root.SetActive(false);
    }

    public void Show()
    {
        if (shown || !GameManager.Instance) return;
        shown = true;

        var gm = GameManager.Instance;

        // Datos (ajusta nombres si tus getters cambian)
        int pasajeros = gm.ObtenerTotalPasajeros();
        float t = gm.ObtenerTiempoTotal();
        int min = Mathf.FloorToInt(t / 60f);
        int seg = Mathf.FloorToInt(t % 60f);

        float dineroBase = gm.ObtenerDineroGanado();   // antes de descuentos
        float descTiempo = gm.ObtenerDescuentoTiempo();
        float descDano = gm.ObtenerDescuentoDano();
        float totalFinal = gm.ObtenerDineroFinal();    // luego de CalcularValores/EndRun

        if (resumenBodyText)
        {
            resumenBodyText.text =
                $"<b>Pasajeros:</b> {pasajeros}\n\n" +
                $"<b>Tiempo total:</b> {min:00}:{seg:00}\n\n" +
                $"<b>Dinero base:</b> ${dineroBase:0.00}\n\n" +
                $"<b>Descuento por tiempo:</b> -${descTiempo:0.00}\n\n" +
                $"<b>Descuento por daño:</b> -${descDano:0.00}\n\n" +
                $"<b>Total:</b> ${totalFinal:0.00}";
        }

        if (root) root.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    // Botones
    public void OnRetry()
    {
        Time.timeScale = 1f; AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnWorkshop()
    {
        Time.timeScale = 1f; AudioListener.pause = false;
        SceneManager.LoadScene("Workshop");
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f; AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }
    void LateUpdate()
    {
        if (root.activeSelf)
        {
            Transform cam = Camera.main.transform;
            Vector3 targetPos = cam.position + cam.forward * 2.0f; // 2m frente a la cámara
            root.transform.position = Vector3.Lerp(root.transform.position, targetPos, 5f * Time.unscaledDeltaTime);
            root.transform.LookAt(cam);
            root.transform.Rotate(0, 180, 0); // para que mire hacia la cámara
        }
    }
}
