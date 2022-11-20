using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TelemetryLib
{

  public class TrackingContext
  {
    string location;        //Optional. ScreenName. Page / Screen. Default=System 
    string category;        //Mandatory. EventCategory. Functionality Eg. FileImport, SysSnapshot
    string action;          //Mandatory. EventAction. Eg. btnFileImport,  AppStartup
    double ? value;         //Optional. EventValue. 
    Dictionary<string, string> values;  //Optional. EventLabel. Multiple event values seperated by comma    
    
    public string Location 
    {
      get {
        return this.location;
      }
      set {
        this.location = value;
      }
    }

    public string Category 
    {
      get {
        return this.category;
      }
      set {
        this.category = value;
      }
    }

    public string Action 
    {
      get {
        return this.action;
      }
      set {
        this.action = value;
      }
    }

    public Dictionary<string, string>  Values 
    {
      get {
        return this.values;
      }
      set {
        this.values = value;
      }
    }

    public double ? Value 
    {
      get {
        return this.value;
      }
      set {
        this.value = value;
      }
    }

    public TrackingContext(string location=null)
    {
      this.location = location;
    }

    public TrackingContext (string category, string action, double value, string location=null)
    {
      this.category = category;
      this.action = action;
      this.value = value;
      this.location = location;
    }
    
    public void setData(string category, string action, double value)
    {
      this.category = category;
      this.action = action;
      this.value = value;
    }

    public void setData(string category, string action, double ? value, Dictionary<string, string> labels = null)
    {
      this.category = category;
      this.action = action;
      this.value = value ;
      this.values = labels;
    }

    public override string ToString()
    {
        string returnValue =  "Location : " + this.location;
        returnValue += ", Category : " + this.category;
        returnValue += ", Name : " + this.action;
        returnValue += ", Value : " + this.value;
        returnValue += ", Label : " + this.values;

        returnValue = JsonConvert.SerializeObject(this);
        return returnValue;
    }  

  }

}
