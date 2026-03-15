using UnityEngine;
using UnityEngine.UI;

public class ScrollableTextController : MonoBehaviour
{
    [Header("Popup Elements")]
    public GameObject popupPanel;
    public ScrollRect scrollRect;
    public Text contentText;

    private CanvasGroup canvasGroup;

    private const string WaterConservationText =
        "Water Conservation\n\n" +
        "Water is the foundation of all life on Earth, yet it is one of our most threatened " +
        "natural resources. Freshwater makes up only about 3% of the Earth's total water supply, " +
        "and of that, less than 1% is readily accessible for human use. As global populations " +
        "grow and climate patterns shift, the demand for clean water is increasing while " +
        "availability in many regions is declining. Glaciers that feed major river systems are " +
        "retreating, groundwater aquifers are being drawn down faster than they can recharge, " +
        "and droughts are becoming more frequent and severe in many parts of the world.\n\n" +
        "Conserving water at the individual level is one of the most impactful actions a person " +
        "can take. Simple changes — fixing leaky faucets, taking shorter showers, running " +
        "dishwashers and washing machines only when full, and choosing drought-resistant plants " +
        "in gardens — can collectively save millions of gallons each year. In agriculture, which " +
        "accounts for roughly 70% of global freshwater withdrawals, adopting drip irrigation, " +
        "crop rotation, and soil moisture monitoring can dramatically reduce waste while " +
        "maintaining or even improving yields.\n\n" +
        "Protecting watersheds and natural water systems is equally critical. Forests, wetlands, " +
        "and grasslands act as natural sponges, absorbing rainfall and slowly releasing it into " +
        "streams and aquifers. When these ecosystems are cleared or degraded, runoff increases, " +
        "flooding becomes more severe, and groundwater recharge rates fall. Investing in the " +
        "restoration of these landscapes is one of the most cost-effective strategies for " +
        "securing long-term water supplies for both people and wildlife.\n\n" +
        "Policy and technology also play essential roles. Smart metering systems help utilities " +
        "and consumers identify leaks and track usage in real time. Desalination and water " +
        "recycling technologies are becoming more efficient and affordable, expanding the range " +
        "of water sources available to communities under stress. Meanwhile, international " +
        "agreements and transboundary water management frameworks are essential for rivers and " +
        "aquifers that cross national borders, where cooperation or conflict over water can " +
        "shape geopolitical stability for decades to come.\n\n" +
        "What You Can Do Today\n\n" +
        "Turn off the tap while brushing your teeth — this saves up to 8 gallons per day. " +
        "Fix dripping faucets promptly; a single faucet dripping once per second wastes over " +
        "3,000 gallons per year. Use a broom instead of a hose to clean driveways and sidewalks. " +
        "Water your garden in the early morning or evening to reduce evaporation. Install " +
        "low-flow showerheads and faucet aerators. Collect rainwater in barrels for garden " +
        "irrigation. Choose native and drought-tolerant plants for landscaping. Run full loads " +
        "in dishwashers and washing machines. Report water main leaks to your local utility " +
        "immediately.\n\n" +
        "The Bigger Picture\n\n" +
        "Every drop of water saved at the household level contributes to a larger reserve that " +
        "sustains ecosystems, agriculture, and industry. Cities that invest in water-efficient " +
        "infrastructure see lower energy costs, reduced strain on treatment facilities, and " +
        "greater resilience during drought emergencies. The choices made today — about how water " +
        "is priced, allocated, protected, and used — will determine whether water remains a " +
        "source of life and prosperity or becomes a source of conflict and scarcity.";

    private void Start()
    {
        if (popupPanel != null)
            canvasGroup = popupPanel.GetComponent<CanvasGroup>();

        if (contentText != null)
            contentText.text = WaterConservationText;

        SetPanelVisible(false);
    }

    public void OpenPopup()
    {
        SetPanelVisible(true);
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    public void ClosePopup()
    {
        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
}
