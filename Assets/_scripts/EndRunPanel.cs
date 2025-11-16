using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;  //  NECESARIO PARA SceneManager

public class EndRunPanel : MonoBehaviour
{
    [Header("Root del panel")]
    public GameObject root;

    [Header("Resumen de texto")]
    public TextMeshProUGUI resumenBodyText;

    bool shown;

    void Start()
    {
        if (root) root.SetActive(false);
    }

    public void Show()
    {
        if (shown || !GameManager.Instance) return;
        shown = true;

        var gm = GameManager.Instance;

        // Datos de la carrera
        int pasajeros = gm.ObtenerTotalPasajeros();
        float t = gm.ObtenerTiempoTotal();
        int min = Mathf.FloorToInt(t / 60f);
        int seg = Mathf.FloorToInt(t % 60f);

        float dineroBase = gm.ObtenerDineroGanado();
        float descTiempo = gm.ObtenerDescuentoTiempo();
        float descDano = gm.ObtenerDescuentoDano();
        float totalFinal = gm.ObtenerDineroFinal();

        //  Sumar esta ganancia al dinero total acumulado
        float dineroTotalAcumulado = 0f;
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddDinero(totalFinal);
            dineroTotalAcumulado = MoneyManager.Instance.DineroTotal;
        }

        if (resumenBodyText)
        {
            resumenBodyText.text =
                $"<b>Pasajeros:</b> {pasajeros}\n\n" +
                $"<b>Tiempo total:</b> {min:00}:{seg:00}\n\n" +
                $"<b>Dinero base:</b> ${dineroBase:0.00}\n\n" +
                $"<b>Descuento por tiempo:</b> -${descTiempo:0.00}\n\n" +
                $"<b>Descuento por daño:</b> -${descDano:0.00}\n\n" +
                $"<b>Total carrera:</b> ${totalFinal:0.00}\n\n" +
                $"<b>Dinero acumulado:</b> ${dineroTotalAcumulado:0.00}\n\n";
        }

        if (root) root.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    // ======== BOTONES ========
    public void OnRetry()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (GameManager.Instance != null)
            GameManager.Instance.ResetRunState();

        if (AsyncSceneFader.Instance != null)
            AsyncSceneFader.Instance.FadeAndLoadScene("Juego");
        else
            SceneManager.LoadScene("Juego");
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (AsyncSceneFader.Instance != null)
            AsyncSceneFader.Instance.FadeAndLoadScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }
}
