   M S B A M L   `   `   `  ����   
TurtleTest    TurtleTest.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  Rosbridge turtle test��$	��480��$	��480��$
��1024��$
��1024��.��� ���#006064 � ColumnDefinitions �� $	��250 ��� �� .+��� -    ��Name$ stackAll��$��8q�$��0�����#80DEEA.+��� -    ��Name$ txtIP��$��10.2.94.144��$��2q��� -   $ txtPort��$
��9090��$��2q��� -    ��Name$ 
btnConnect��$��Connect��
��� E��$��2q��� -   $ stackControls��$��8q����#0087A7.+��� -   $ btnSubscribe��$��Subcribe��$��2q��� -    ��Name$ lblTargetState��$��Target is idle���� -	   $ 
btnForward��$��Forward��$��2q��� -
   $ btnBackward��$��Backward��$��2q��� -   $ btnLeft��$
��Left��$��2q��� -   $ btnRight��$��Right��$��2q��� -   $ btnStop��$
��Stop��$��2q��� $��Length of velocity vector���� -   $ txtLgth��$��1��$��2q��� $��Degree of rotation��$��2q��� -   $ txtTheta��$��90��$��2q��� -   $ labelX������ *msg.x+�� -   $ labelY������ *msg.y+�� -   $ 
labelTheta������ *
receiver+� $��1�����#00B8D4 � RowDefinitions �� $��50 ��� $��* ��� $��* �.+��� $��Laser scanner read ranges���� -    ��Name$ sensor_projection��$��1��$��2 2 2 2q����#E0F7FA�� -   $ laser_field��$��2��$��	150 2 2 2q����#E0F2F1� $��2�����#00ACC1 �� �� .+��� -    ��Name$ kinect_screen��$��0���� -   $ kinect_depth_screen��$��1��