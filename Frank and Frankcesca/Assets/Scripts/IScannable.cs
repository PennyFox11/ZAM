
//Akhona Khoali 
//logic for scannable objects


public interface IScannable
{
    void OnScanStart();    // The beam just started touching this object.
    void OnScanEnd();      // The beam stopped touching this object.
}