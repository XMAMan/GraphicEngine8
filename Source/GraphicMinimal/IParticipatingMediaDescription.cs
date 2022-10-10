using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphicMinimal
{
    public interface IParticipatingMediaDescription
    {
        IParticipatingMediaDescription Clone();
        int Priority { get; set; }//Wenn mehrere Media-Objekte inneinander verschachtelt sind(Im Luftwürfel befindet sich 
                                  //Wasser), dann bekommt Luftwürfel die 2 und das Wasser die 3. Das innenliegende bekommt die höhere Zahl.
                                  //Wenn hier 0 angegeben wird, dann wird Priority nach aufsteigender Reihenfolge vergeben
    }

    //Wenn man ein Glas-Würfel ohne Media machen will, dann braucht dieser eine Media-Priority > 2, damit der Brechungsindex vom Würfel verwendet wird
    public class DescriptionForVacuumMedia : IParticipatingMediaDescription
    {
        public int Priority { get; set; } = 0;
        public DescriptionForVacuumMedia() { }

        public IParticipatingMediaDescription Clone()
        {
            return new DescriptionForVacuumMedia();
        }
    }

    
    public class DescriptionForHomogeneousMedia : IParticipatingMediaDescription
    {
        public int Priority { get; set; } = 0; 
        public Vector3D AbsorbationCoeffizent { get; set; } //Diese Zahl darf von 0 bis Unendlich gehen. Bestimmt die Helligkeit vom Medium/von den Flächen, die am Medium angrenzen
        public Vector3D EmissionCoeffizient { get; set; }
        public Vector3D ScatteringCoeffizent { get; set; } //Hier sollten nur zahlen von 0 bis 1 eingesetzt werden (Bestimmt die Farbe/Dichtigkeit des Mediums)
        public float AnisotropyCoeffizient { get; set; } //0 = Isoscattering; != 0 Anisoscattering (Zahl im Bereich von -1 bis +1)
        public float PhaseFunctionExtraFactor { get; set; } = 1; //Sobald hier ein Wert != 1 angegeben wird, wird verletzt die Phasenfunktion den Energieerhaltungssatz aber so kann man schöne Effekte machen

        public DescriptionForHomogeneousMedia() 
        {
            this.AbsorbationCoeffizent = new Vector3D(0, 0, 0);
            this.EmissionCoeffizient = new Vector3D(0, 0, 0);
            this.ScatteringCoeffizent = new Vector3D(0, 0, 0);
            this.AnisotropyCoeffizient = 0;
            this.PhaseFunctionExtraFactor = 1;
        }

        public DescriptionForHomogeneousMedia(DescriptionForHomogeneousMedia copy)
        {
            this.AbsorbationCoeffizent = new Vector3D(copy.AbsorbationCoeffizent);
            this.EmissionCoeffizient = new Vector3D(copy.EmissionCoeffizient);
            this.ScatteringCoeffizent = new Vector3D(copy.ScatteringCoeffizent);
            this.AnisotropyCoeffizient = copy.AnisotropyCoeffizient;
            this.PhaseFunctionExtraFactor = copy.PhaseFunctionExtraFactor;
        }

        public IParticipatingMediaDescription Clone()
        {
            return new DescriptionForHomogeneousMedia(this);
        }
    }

    //Quelle für diese Zahlen: https://www.scratchapixel.com/lessons/procedural-generation-virtual-worlds/simulating-sky
    //Es wird angenommen, dass die Erde aus zwei Luftschichten besteht: Mie(Staubschicht ganz unten) und Rayleigh (Luftschicht darüber)
    //Wärend man die Transmittance berechnet, geht man davon aus, das beide Schicht bei der Erde beginnen und bis komplett zum AtmosphereRadius reichen,
    //welche die allerletzte Luftschicht (Es gibt mehr als 2 Schichten), welche hier garnicht im Model mit auftaucht, umschließt.
    public class DescriptionForSkyMedia : IParticipatingMediaDescription
    {
        public int Priority { get; set; } = 0;
        public float EarthRadius = 6360000;
        public float AtmosphereRadius = 6420000;
        public float RayleighScaleHeight = 7994; //So viel Meter über der Mia-Schicht hat die Dichte der Rayleigh-Schicht ein definierten Dichtewert X
        public float MieScaleHeight = 1200; //So viel Meter über der Erde hat die Dichte der Mie-Schicht ein definierten Dichtewert X
        public Vector3D SunDirection = new Vector3D(0, 1, 0); //Aus dieser Richtung scheint die Sonne. Es wird angenommen, dass des Richtungslicht ist
        public Vector3D CenterOfEarth = new Vector3D(0, 0, 0); //Mittelpunkt der Erde
        public Vector3D RayleighScatteringCoeffizientOnSeaLevel = new Vector3D(3.8e-6f, 13.5e-6f, 33.1e-6f); //Luft-Partikeldichte auf Meereshöhe
        public Vector3D MieScatteringCoeffizientOnSeaLevel = new Vector3D(21e-6f, 21e-6f, 21e-6f); //Staub-Partikeldichte auf Meereshöhe
        public float MieAnisotrophieCoeffizient = 0.76f;

        public DescriptionForSkyMedia()
        {
        }

        public DescriptionForSkyMedia(DescriptionForSkyMedia copy)
        {
            this.EarthRadius = copy.EarthRadius;
            this.AtmosphereRadius = copy.AtmosphereRadius;
            this.RayleighScaleHeight = copy.RayleighScaleHeight;
            this.MieScaleHeight = copy.MieScaleHeight;
            this.SunDirection = new Vector3D(copy.SunDirection);
            this.CenterOfEarth = new Vector3D(copy.CenterOfEarth);
        }

        public IParticipatingMediaDescription Clone()
        {
            return new DescriptionForSkyMedia(this);
        }
    }

    public class DescriptionForDensityFieldMedia
    {
        public Vector3D ScatteringCoeffizent { get; set; } = new Vector3D(1, 1, 1);
        public Vector3D AbsorbationCoeffizent { get; set; } = new Vector3D(1, 1, 1) * 0.5f; //Diese Zahl darf von 0 bis Unendlich gehen.
        public float AnisotropyCoeffizient { get; set; } //0 = Isoscattering; != 0 Anisoscattering (Zahl im Bereich von -1 bis +1)
        public int StepCountForAttenuationIntegration { get; set; } = 20; //Wenn das Medium homogen ist, kann diese Zahl hier auf 1 gesetzt werden

        public DescriptionForDensityFieldMedia() { }
        public DescriptionForDensityFieldMedia(DescriptionForDensityFieldMedia copy)
        {
            this.ScatteringCoeffizent = copy.ScatteringCoeffizent;
            this.AbsorbationCoeffizent = copy.AbsorbationCoeffizent;
            this.AnisotropyCoeffizient = copy.AnisotropyCoeffizient;
            this.StepCountForAttenuationIntegration = copy.StepCountForAttenuationIntegration;
        }
    }

    //Wolke laut den Modell von David Ebert (Siehe Perlin-Mitschriften.txt Funktion 'cumulus'; Forschungen\ParticipatingMedia_2019\Clouds\Ebert Perlin Texturing and Modeling a Procedural Approach 1998\Ebert Perlin Texturing and Modeling a Procedural Approach 1998.pdf Seite 302)
    public class DescriptionForCloudMedia : DescriptionForDensityFieldMedia, IParticipatingMediaDescription
    {
        public enum CloudDrawingObject { AxialCube, Sphere}
        public int Priority { get; set; } = 0;

        public int RandomSeed = 1;
        public int MinMetaballCount = 5; //Inclusive Untergrenze
        public int MaxMetaballCount = 6; //Exklusive Obergrenze
        public float DensityScalingFactor = 0.5f; //0.2 .. 0.4
        public float PowExponent = 0.5f;    //Exponent der pow-Funktion (Um so mehr gegen 0, um so größer wird die Wolke;
        public float BlendingBetweenMetaballAndTurbulence = 0.4f; //Blendwert zwischen implicit-Metaballs und Turbulence
        public float TurbulenceFactor = 0.7f;//Größe der Turbulence, um Abfragepunkt vor Nutzung der implicit-Funktion zu verschieben (Um so größer, um so weniger wird die Wolke)

        
        public CloudDrawingObject ShellType = CloudDrawingObject.Sphere;

        public DescriptionForCloudMedia()
        {
        }
        public DescriptionForCloudMedia(DescriptionForCloudMedia copy)
            :base(copy)
        {
            this.Priority = copy.Priority;
            this.RandomSeed = copy.RandomSeed;
            this.MinMetaballCount = copy.MinMetaballCount;
            this.MaxMetaballCount = copy.MaxMetaballCount;
            this.DensityScalingFactor = copy.DensityScalingFactor;
            this.PowExponent = copy.PowExponent;
            this.BlendingBetweenMetaballAndTurbulence = copy.BlendingBetweenMetaballAndTurbulence;
            this.TurbulenceFactor = copy.TurbulenceFactor;
        }

        public IParticipatingMediaDescription Clone()
        {
            return new DescriptionForCloudMedia(this);
        }
    }

    public class DescriptionForRisingSmokeMedia : DescriptionForDensityFieldMedia, IParticipatingMediaDescription
    {
        public int Priority { get; set; } = 0;
        public int RandomSeed = 1;
        public float MinRadius = 0.3f; //Geht von 0 .. 1; 1 = ZylinderRadius (Aus diesen Kreis am Boden kommt der Rauch herraus)
        public float MaxRadius = 0.8f; //Geht von 0 .. 1; 1 = ZylinderRadius (Bis zu diesen Kreis ganz oben geht der Rauch)
        public float Turbulence = 2; //So viel weicht die Form von der perfekten Kegel-Form ab
        public Vector2D WindDirection = new Vector2D(1, 0); //Angabe in der XZ-Ebene
        public DescriptionForRisingSmokeMedia()
        {
        }
        public DescriptionForRisingSmokeMedia(DescriptionForRisingSmokeMedia copy)
            : base(copy)
        {
            this.Priority = copy.Priority;
            this.RandomSeed = copy.RandomSeed;
        }

        public IParticipatingMediaDescription Clone()
        {
            return new DescriptionForRisingSmokeMedia(this);
        }
    }

    public class DescriptionForRisingSmokeMedia1 : DescriptionForDensityFieldMedia, IParticipatingMediaDescription
    {
        public int Priority { get; set; } = 0;
        public int RandomSeed = 1;
        public float MinRadius = 0.3f; //Geht von 0 .. 1; 1 = ZylinderRadius
        public float HelixCount = 5; //So viele Spiralrunden gibt es
        public DescriptionForRisingSmokeMedia1()
        {
        }
        public DescriptionForRisingSmokeMedia1(DescriptionForRisingSmokeMedia1 copy)
            : base(copy)
        {
            this.Priority = copy.Priority;
            this.RandomSeed = copy.RandomSeed;
        }

        public IParticipatingMediaDescription Clone()
        {
            return new DescriptionForRisingSmokeMedia1(this);
        }
    }

    public class DescriptionForGridCloudMedia : DescriptionForDensityFieldMedia, IParticipatingMediaDescription
    {
        public int Priority { get; set; } = 0;
        public LegoGrid LegoGrid { get; set; }
        public int RandomSeed = 1;

        public DescriptionForGridCloudMedia() { }

        public DescriptionForGridCloudMedia(DescriptionForGridCloudMedia copy)
            : base(copy)
        {
            this.Priority = copy.Priority;
            this.LegoGrid = copy.LegoGrid;
            this.RandomSeed = copy.RandomSeed;
        }

        public IParticipatingMediaDescription Clone()
        {
            return new DescriptionForGridCloudMedia(this);
        }
    }
}
