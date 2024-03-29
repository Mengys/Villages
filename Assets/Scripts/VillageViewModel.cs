//using System.Collections;
//using System.Collections.Generic;
//using Unity.Netcode;
//using UnityEngine;

//public class VillageViewModel
//{
//    private VillageModel _model;

//    public ReactiveProperty<int> CounterView = new();

//    public VillageViewModel(VillageModel model) {
//        _model = model;

//        _model.Counter.OnChanged += OnModelCounterChanged;
//    }

//    private void OnModelCounterChanged(int value) {
//        CounterView.Value = value;
//    }
//}
