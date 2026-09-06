{{- define "jtldemo-linux.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "jtldemo-linux.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name (include "jtldemo-linux.name" .) | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}

{{- define "jtldemo-linux.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "jtldemo-linux.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{- define "jtldemo-linux.secretProviderClassName" -}}
{{- printf "%s-keyvault" (include "jtldemo-linux.fullname" .) | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "jtldemo-linux.image" -}}
{{- if .Values.image.digest -}}
{{- printf "%s@%s" .Values.image.repository .Values.image.digest -}}
{{- else -}}
{{- printf "%s:%s" .Values.image.repository (required "image.tag is required without image.digest" .Values.image.tag) -}}
{{- end -}}
{{- end -}}

{{- define "jtldemo-linux.labels" -}}
app.kubernetes.io/name: {{ include "jtldemo-linux.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" }}
{{- end }}

{{- define "jtldemo-linux.selectorLabels" -}}
app.kubernetes.io/name: {{ include "jtldemo-linux.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
