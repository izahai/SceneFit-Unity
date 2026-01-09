using System;
using System.Collections.Generic;

[Serializable]
public class ClothesResponse
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

[Serializable]
public class AllMethodsResponse
{
    public ApproachResponse approach_1;
    public ApproachResponse approach_2;
    public ApproachResponse approach_3;
}

[Serializable]
public class ApproachResponse
{
    public string bg_caption;
    public string[] query;
    public ClothingResult result;
}
