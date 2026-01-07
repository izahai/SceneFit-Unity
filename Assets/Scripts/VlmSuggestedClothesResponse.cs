using System;
using System.Collections.Generic;

[Serializable]
public class VlmSuggestedClothesResponse
{
    public string[] query;
    public List<ClothingResult> results;
}

[Serializable]
public class ClothingResult
{
    public string name_clothes;
    public float similarity;
    public string best_description;
}
