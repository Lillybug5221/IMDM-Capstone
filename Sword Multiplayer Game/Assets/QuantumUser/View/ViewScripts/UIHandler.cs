using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Photon.Deterministic;
using UnityEngine.UI;
using TMPro;

public class UIHandler : MonoBehaviour
{
    public Image Player1HealthBar;
    public Image Player1StanceBar;
    public Image Player2HealthBar;
    public Image Player2StanceBar;
    public TextMeshProUGUI ScoreText;


    public void OnEnable(){
        QuantumEvent.Subscribe<EventBarChange>(listener: this, handler: (EventBarChange e) => UpdateBar(e));
        QuantumEvent.Subscribe<EventUpdateScore>(listener: this, handler: (EventUpdateScore e) => UpdateScore(e));
    }

    public void OnDisbale(){
        QuantumEvent.UnsubscribeListener(this);
    }
    

    private void UpdateBar(EventBarChange e){
        if(QuantumRunner.Default.Game.PlayerIsLocal(e.Player)){
            if(e.BarNum == 0){
                Player1HealthBar.rectTransform.localScale = new Vector3((float)(e.NewValue/e.MaxValue),1,1);
            }else{
                Player1StanceBar.rectTransform.localScale = new Vector3((float)(e.NewValue/e.MaxValue),1,1);
            }
        }else{
            if(e.BarNum == 0){
                Player2HealthBar.rectTransform.localScale = new Vector3((float)(e.NewValue/e.MaxValue),1,1);
            }else{
                Player2StanceBar.rectTransform.localScale = new Vector3((float)(e.NewValue/e.MaxValue),1,1);
            }
        }
        
    }

    private void UpdateScore(EventUpdateScore e){
        if(QuantumRunner.Default.Game.PlayerIsLocal(e.Player1Player)){
            ScoreText.text = "" + e.Player1Score + " - " + e.Player2Score;
        }else{
            ScoreText.text = "" + e.Player2Score + " - " + e.Player1Score;
        }
        
    }
}
