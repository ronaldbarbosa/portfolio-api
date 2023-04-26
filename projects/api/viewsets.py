from rest_framework import viewsets

from projects.api import serializers
from projects import models

class ProjectsViewSet(viewsets.ModelViewSet):
    serializer_class = serializers.ProjectSerializer
    queryset = models.Project.objects.all()
