using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PocketMapController : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject[] pages;

    [Header("Page Flip")]
    [SerializeField] private Image pageFlipImage;
    [SerializeField] private Sprite[] nextPageFrames;
    [SerializeField] private Sprite[] previousPageFrames;

    [Header("Open / Close Notebook")]
    [SerializeField] private Image notebookAnimationImage;
    [SerializeField] private Sprite[] notebookCloseFrames;

    [Header("Timing")]
    [SerializeField] private float pageFlipFrameTime = 0.045f;
    [SerializeField] private float notebookFrameTime = 0.055f;

    private int currentPage = 0;
    private bool isAnimating = false;

    public int CurrentPage => currentPage;
    public bool IsAnimating => isAnimating;

    // =========================================================
    // SETUP
    // =========================================================

    public void Setup(
        GameObject[] newPages,
        Image newPageFlipImage,
        Sprite[] newNextFrames,
        Sprite[] newPreviousFrames,
        Image newNotebookAnimationImage,
        Sprite[] newNotebookCloseFrames
    )
    {
        pages = newPages;

        pageFlipImage = newPageFlipImage;
        nextPageFrames = newNextFrames;
        previousPageFrames = newPreviousFrames;

        notebookAnimationImage = newNotebookAnimationImage;
        notebookCloseFrames = newNotebookCloseFrames;

        currentPage = 0;

        ShowOnlyPage(0);

        if (pageFlipImage != null)
        {
            pageFlipImage.gameObject.SetActive(false);
        }

        if (notebookAnimationImage != null)
        {
            notebookAnimationImage.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // CAMBIAR PÁGINA
    // =========================================================

    public void OpenPage(int pageIndex)
    {
        if (isAnimating)
            return;

        if (pages == null || pages.Length == 0)
            return;

        if (pageIndex < 0 || pageIndex >= pages.Length)
            return;

        if (pageIndex == currentPage)
            return;

        bool forward = pageIndex > currentPage;

        StartCoroutine(
            ChangePageRoutine(
                pageIndex,
                forward
            )
        );
    }

    private IEnumerator ChangePageRoutine(
        int newPage,
        bool forward
    )
    {
        isAnimating = true;

        Sprite[] frames =
            forward
                ? nextPageFrames
                : previousPageFrames;

        if (
            pageFlipImage != null &&
            frames != null &&
            frames.Length > 0
        )
        {
            pageFlipImage.gameObject.SetActive(true);

            int midpoint = frames.Length / 2;

            for (int i = 0; i < frames.Length; i++)
            {
                pageFlipImage.sprite = frames[i];

                // Cuando la hoja tapa suficientemente la página,
                // cambiamos los datos debajo.
                if (i == midpoint)
                {
                    ShowOnlyPage(newPage);
                }

                yield return new WaitForSecondsRealtime(
                    pageFlipFrameTime
                );
            }

            pageFlipImage.gameObject.SetActive(false);
        }
        else
        {
            ShowOnlyPage(newPage);
        }

        currentPage = newPage;
        isAnimating = false;
    }

    // =========================================================
    // CERRAR LIBRETA COMPLETA
    // =========================================================

    public IEnumerator PlayNotebookCloseAnimation()
    {
        if (isAnimating)
            yield break;

        isAnimating = true;

        if (
            notebookAnimationImage != null &&
            notebookCloseFrames != null &&
            notebookCloseFrames.Length > 0
        )
        {
            notebookAnimationImage.gameObject.SetActive(true);

            for (int i = 0; i < notebookCloseFrames.Length; i++)
            {
                notebookAnimationImage.sprite =
                    notebookCloseFrames[i];

                yield return new WaitForSecondsRealtime(
                    notebookFrameTime
                );
            }
        }

        isAnimating = false;
    }

    // =========================================================
    // ABRIR LIBRETA COMPLETA
    // =========================================================

    public IEnumerator PlayNotebookOpenAnimation()
    {
        if (isAnimating)
            yield break;

        isAnimating = true;

        if (
            notebookAnimationImage != null &&
            notebookCloseFrames != null &&
            notebookCloseFrames.Length > 0
        )
        {
            notebookAnimationImage.gameObject.SetActive(true);

            for (
                int i = notebookCloseFrames.Length - 1;
                i >= 0;
                i--
            )
            {
                notebookAnimationImage.sprite =
                    notebookCloseFrames[i];

                yield return new WaitForSecondsRealtime(
                    notebookFrameTime
                );
            }

            notebookAnimationImage.gameObject.SetActive(false);
        }

        isAnimating = false;
    }

    // =========================================================
    // ESTADO DE PÁGINA
    // =========================================================

    public void SetCurrentPageInstant(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
            return;

        pageIndex = Mathf.Clamp(
            pageIndex,
            0,
            pages.Length - 1
        );

        currentPage = pageIndex;

        ShowOnlyPage(pageIndex);
    }

    private void ShowOnlyPage(int pageIndex)
    {
        if (pages == null)
            return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == pageIndex);
            }
        }
    }
}