using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class OficinaMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI monedasText;

    private void Start()
    {
        ActualizarMonedasUI();
    }

    void ActualizarMonedasUI()
    {
        if (monedasText == null) return;

        float total = 0f;

        if (MoneyManager.Instance != null)
        {
            total = MoneyManager.Instance.DineroTotal;
        }
        else
        {
            // Por si entraste directo a Oficina sin MoneyManager en escena
            total = PlayerPrefs.GetFloat("DINERO_TOTAL", 0f);
        }

        monedasText.text = total.ToString("0.00");
    }

    // BOTÓN: EMPEZAR (nuevo juego, dinero reset)
    public void OnEmpezar()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.ResetDinero();
        }
        else
        {
            PlayerPrefs.SetFloat("DINERO_TOTAL", 0f);
            PlayerPrefs.Save();
        }

        // Ir a la escena Juego desde cero
        if (AsyncSceneFader.Instance != null)
            AsyncSceneFader.Instance.FadeAndLoadScene("Juego");
        else
            SceneManager.LoadScene("Juego");
    }

    // BOTÓN: CARGAR (con dinero guardado)
    public void OnCargar()
    {
        // No tocamos el dinero, solo vamos a la escena de juego
        if (AsyncSceneFader.Instance != null)
            AsyncSceneFader.Instance.FadeAndLoadScene("Juego");
        else
            SceneManager.LoadScene("Juego");
    }
}
